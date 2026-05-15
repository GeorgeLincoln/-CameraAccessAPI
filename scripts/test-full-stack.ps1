$ErrorActionPreference = "Stop"

param(
    [string]$ApiBaseUrl = "http://localhost:5001",
    [string]$DbContainer = "camera_access_db",
    [string]$JwtKey = "CameraAccessApiLocalDevJwtSecretKey123!",
    [string]$Issuer = "CameraAccessAPI",
    [string]$Audience = "CameraClients",
    [string]$CameraName = "test",
    [string]$UserName = "Jessica Test User"
)

function ConvertTo-Base64Url([byte[]]$Bytes) {
    [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function New-Hs256Jwt {
    param(
        [string]$Sub,
        [string]$Camera,
        [string]$Key,
        [int]$ExpiryMinutes = 30
    )

    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = [DateTimeOffset]::UtcNow.AddMinutes($ExpiryMinutes).ToUnixTimeSeconds()

    $headerJson = '{"alg":"HS256","typ":"JWT"}'
    $payloadObj = @{
        sub    = $Sub
        camera = $Camera
        jti    = [Guid]::NewGuid().ToString()
        iat    = $now
        nbf    = $now
        exp    = $exp
        iss    = $Issuer
        aud    = $Audience
    }

    $payloadJson = $payloadObj | ConvertTo-Json -Compress

    $header = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes($headerJson))
    $payload = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes($payloadJson))
    $unsignedToken = "$header.$payload"

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($Key))
    $signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsignedToken))
    $signature = ConvertTo-Base64Url $signatureBytes

    return "$unsignedToken.$signature"
}

Write-Host "1) Verificando health da API..."
$health = Invoke-RestMethod -Uri "$ApiBaseUrl/health" -Method Get
Write-Host "Health OK: $health"

Write-Host "2) Inserindo usuário de teste diretamente no banco (bootstrap)..."
$userId = [Guid]::NewGuid().ToString()
$document = "DOC-$($userId.Substring(0,8))"
$insertUserSql = @"
INSERT INTO "Users" ("Id","Name","Document","Active","CreatedAt","UpdatedAt")
VALUES ('$userId','$UserName','$document',TRUE,NOW(),NOW())
ON CONFLICT ("Id") DO NOTHING;
"@
docker exec $DbContainer psql -U postgres -d CameraAccessDb -c $insertUserSql | Out-Null

Write-Host "3) Gerando token Bearer local para chamadas protegidas..."
$adminToken = New-Hs256Jwt -Sub $userId -Camera $CameraName -Key $JwtKey -ExpiryMinutes 30
$headers = @{ Authorization = "Bearer $adminToken" }

Write-Host "4) Criando câmera (se não existir)..."
$cameraBody = @{
    name = $CameraName
    description = "Camera de teste E2E"
    location = "Lab"
    rtspUrl = "rtsp://host.docker.internal:8554/$CameraName"
    active = $true
} | ConvertTo-Json

try {
    $createdCamera = Invoke-RestMethod -Uri "$ApiBaseUrl/api/cameras" -Method Post -Headers $headers -ContentType "application/json" -Body $cameraBody
    $cameraId = $createdCamera.id
}
catch {
    $existing = Invoke-RestMethod -Uri "$ApiBaseUrl/api/cameras" -Method Get -Headers $headers
    $match = $existing | Where-Object { $_.name -eq $CameraName } | Select-Object -First 1
    if (-not $match) { throw "Camera $CameraName não encontrada e criação falhou." }
    $cameraId = $match.id
}

Write-Host "5) Vinculando usuário à câmera..."
$linkBody = '"' + $cameraId + '"'
Invoke-RestMethod -Uri "$ApiBaseUrl/api/users/$userId/cameras" -Method Post -Headers $headers -ContentType "application/json" -Body $linkBody | Out-Null

Write-Host "6) Criando regra de acesso..."
$ruleBody = @{
    userId = $userId
    cameraId = $cameraId
    allowed = $true
    days = @(0,1,2,3,4,5,6)
    schedules = @(
        @{
            startTime = "00:00:00"
            endTime = "23:59:59"
        }
    )
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "$ApiBaseUrl/access-rules" -Method Post -Headers $headers -ContentType "application/json" -Body $ruleBody | Out-Null

Write-Host "7) Obtendo URL assinada de stream via /watch..."
$watchResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/watch?camera=$CameraName" -Method Get -Headers $headers
$streamUrl = $watchResponse.streamUrl
Write-Host "Stream URL:"
Write-Host $streamUrl

$uri = [System.Uri]$streamUrl
$queryParts = $uri.Query.TrimStart("?").Split("&", [System.StringSplitOptions]::RemoveEmptyEntries)
$tokenPair = $queryParts | Where-Object { $_ -like "token=*" } | Select-Object -First 1
if (-not $tokenPair) { throw "Token não encontrado na streamUrl." }
$streamToken = $tokenPair.Substring("token=".Length)

Write-Host "8) Validando token no endpoint de stream auth..."
$validateResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/access/stream/validate?stream=$CameraName&token=$streamToken&hookSecret=local-mediamtx-hook-secret-2026" -Method Post
Write-Host "Validação stream:"
$validateResponse | ConvertTo-Json

Write-Host ""
Write-Host "==== RESULTADO ===="
Write-Host "UserId:    $userId"
Write-Host "CameraId:  $cameraId"
Write-Host "Token:     $adminToken"
Write-Host "StreamUrl: $streamUrl"
Write-Host ""
Write-Host "Agora teste no player:"
Write-Host "http://localhost:8888/$CameraName?token=$streamToken"
