# Unity MCP (mcp-for-unity) helper - Mask Dimensions project
# Bridge: http://127.0.0.1:8092 (Streamable HTTP / FastMCP)
# Usage (in pwsh):
#   . .superpowers/mcp/unity-mcp-helper.ps1      # load functions
#   Invoke-McpConnect                            # handshake + select Mask Dimensions instance (auto retry)
#   Invoke-McpCode 'return "hello";'             # run a snippet of editor C# and print result
# Note: session id is persisted in %TEMP%\dsh_mcp_session.txt (auto refreshed by connect).

$script:McpUrl = "http://127.0.0.1:8092/mcp"
$script:McpSessionFile = Join-Path $env:TEMP "dsh_mcp_session.txt"

function Invoke-McpPost {
    param([int]$Id, [string]$Method, $Params, [switch]$NoId)
    $sid = ""
    if (Test-Path $script:McpSessionFile) { $sid = Get-Content $script:McpSessionFile -Raw -ErrorAction SilentlyContinue }
    $bodyObj = @{ jsonrpc = "2.0"; method = $Method }
    if (-not $NoId) { $bodyObj.id = $Id }
    if ($null -ne $Params) { $bodyObj.params = $Params }
    $body = $bodyObj | ConvertTo-Json -Depth 12 -Compress
    $headers = @{ Accept = "application/json, text/event-stream" }
    if ($sid -ne "") { $headers["mcp-session-id"] = $sid }
    $resp = Invoke-WebRequest -Uri $script:McpUrl -Method POST -ContentType "application/json" -Headers $headers -Body $body -UseBasicParsing -TimeoutSec 60
    $ct = [string]$resp.Headers['Content-Type']
    if ($ct -like "*text/event-stream*") {
        $msgs = @()
        foreach ($line in ($resp.Content -split "`n")) {
            $t = $line.Trim()
            if ($t -like "data:*") { $msgs += $t.Substring(5).Trim() }
        }
        return ($msgs -join "`n")
    }
    return $resp.Content
}

function Invoke-McpCode {
    param([string]$Code)
    $r = Invoke-McpPost -Id (Get-Random -Maximum 20000) -Method "tools/call" -Params @{ name = "execute_code"; arguments = @{ action = "execute"; code = $Code } }
    try {
        $obj = $r | ConvertFrom-Json
        if ($obj.result.structuredContent) { return $obj.result.structuredContent }
        return $obj.result.content[0].text
    } catch { return $r }
}

function Invoke-McpConnect {
    # 1) handshake (fresh session overwrites old file; retry twice; fallback to old session)
    $body = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"dsh-debug","version":"1.0"}}}'
    $sid = ""
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        try {
            $resp = Invoke-WebRequest -Uri $script:McpUrl -Method POST -ContentType "application/json" -Headers @{ Accept = "application/json, text/event-stream" } -Body $body -UseBasicParsing -TimeoutSec 15
            $sid = $resp.Headers['mcp-session-id']
            if (-not $sid) { $sid = $resp.Headers['Mcp-Session-Id'] }
            if ($sid) { Set-Content -LiteralPath $script:McpSessionFile -Value $sid -Encoding ascii; break }
        } catch { Start-Sleep -Milliseconds 400 }
    }
    if ($sid -ne "") {
        Write-Host ("session OK: " + $sid)
        Invoke-McpPost -Method "notifications/initialized" -Params @{} -NoId | Out-Null
    } elseif (Test-Path $script:McpSessionFile) {
        Write-Host "handshake failed; reusing old session (may be stale)"
    } else {
        Write-Host "NO SESSION"
        return $false
    }
    # 2) list instances, select Mask Dimensions
    $inst = Invoke-McpPost -Id 2 -Method "resources/read" -Params @{ uri = "mcpforunity://instances" }
    $text = ""
    foreach ($m in ($inst -split "`n")) {
        try { $o = $m | ConvertFrom-Json; if ($o.result.contents) { $text = $o.result.contents[0].text } } catch {}
    }
    if ($text -eq "") { Write-Host "instances parse failed"; return $false }
    $data = $text | ConvertFrom-Json
    $data.instances | ForEach-Object { Write-Host ("instance: " + $_.id) }
    $target = ($data.instances | Where-Object { $_.name -like "Mask Dimensions*" } | Select-Object -First 1)
    if ($target -eq $null) { Write-Host "Mask Dimensions instance NOT connected"; return $false }
    Invoke-McpPost -Id 10 -Method "tools/call" -Params @{ name = "set_active_instance"; arguments = @{ instance = $target.id } } | Out-Null
    Write-Host ("active instance -> " + $target.id)
    return $true
}