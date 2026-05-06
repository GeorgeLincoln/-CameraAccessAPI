# Script de Debug e Troubleshooting - PostgreSQL Connection

# Cores para output
$colors = @{
    'green' = @{ foreground = 'Green'; }
    'red'   = @{ foreground = 'Red'; }
    'yellow'= @{ foreground = 'Yellow'; }
    'blue'  = @{ foreground = 'Cyan'; }
}

function Show-Header {
    param([string]$text)
    Write-Host "`n$('='*60)" -ForegroundColor Blue
    Write-Host $text -ForegroundColor Blue
    Write-Host $('='*60) -ForegroundColor Blue
}

function Test-DockerRunning {
    Show-Header "1. Verificando se Docker esta rodando..."
    
    try {
        $result = docker ps 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Docker esta rodando!" -ForegroundColor Green
            return $true
        }
    } catch {
        Write-Host "Docker nao esta respondendo" -ForegroundColor Red
    }
    return $false
}

function Test-PostgresContainer {
    Show-Header "2. Verificando container PostgreSQL..."
    
    $container = docker ps --filter "name=camera_access_db" --format "{{.Names}}"
    
    if ($container) {
        Write-Host "Container 'camera_access_db' esta rodando!" -ForegroundColor Green
        return $true
    } else {
        Write-Host "Container nao encontrado" -ForegroundColor Red
        Write-Host "`nIniciando docker-compose..." -ForegroundColor Yellow
        docker-compose up -d
        Start-Sleep -Seconds 5
        return $false
    }
}

function Test-DatabaseConnection {
    Show-Header "3. Testando conexao com PostgreSQL..."
    
    try {
        $result = docker exec camera_access_db psql -U postgres -c "SELECT 1;" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Conexao com PostgreSQL OK!" -ForegroundColor Green
            return $true
        }
    } catch {
        Write-Host "Falha ao conectar: $_" -ForegroundColor Red
    }
    return $false
}

function Test-Database {
    Show-Header "4. Verificando banco de dados..."
    
    try {
        $result = docker exec camera_access_db psql -U postgres -c "SELECT 1 FROM pg_database WHERE datname='CameraAccessDb';" 2>&1
        if ($result -match "1 row") {
            Write-Host "Banco de dados 'CameraAccessDb' existe!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "Banco de dados nao encontrado. Criando..." -ForegroundColor Yellow
            docker exec camera_access_db psql -U postgres -c "CREATE DATABASE CameraAccessDb;" 2>&1
            Start-Sleep -Seconds 2
            return $true
        }
    } catch {
        Write-Host "Erro ao verificar banco: $_" -ForegroundColor Red
    }
    return $false
}

function Show-Logs {
    Show-Header "5. Ultimos logs do PostgreSQL..."
    docker-compose logs postgres --tail 20
}

function Show-Configuration {
    Show-Header "6. Configuracao da Conexao"
    Write-Host "`nArquivo: appsettings.Development.json" -ForegroundColor Cyan
    
    $config = Get-Content "appsettings.Development.json" | ConvertFrom-Json
    $connString = $config.ConnectionStrings.DefaultConnection
    
    Write-Host "Connection String:" -ForegroundColor Yellow
    Write-Host "  $connString" -ForegroundColor White
    
    # Extrair componentes
    if ($connString -match "Host=([^;]+).*Username=([^;]+).*Password=([^;]+).*Database=([^;]+)") {
        $host = $matches[1]
        $user = $matches[2]
        $db = $matches[4]
        
        Write-Host "`nComponentes:" -ForegroundColor Yellow
        Write-Host "  Host: $host" -ForegroundColor White
        Write-Host "  User: $user" -ForegroundColor White
        Write-Host "  Database: $db" -ForegroundColor White
        Write-Host "  Password: ***hidden***" -ForegroundColor White
    }
}

function Test-DotnetConnection {
    Show-Header "7. Testando conexao via .NET..."
    
    Write-Host "Compilando projeto..." -ForegroundColor Yellow
    dotnet build 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build bem-sucedido!" -ForegroundColor Green
        Write-Host "`nIniciando aplicacao..." -ForegroundColor Yellow
        dotnet run 2>&1 | Select-Object -First 100
        return $true
    } else {
        Write-Host "Erro na compilacao" -ForegroundColor Red
        return $false
    }
}

function Show-Summary {
    Show-Header "RESUMO DO DIAGNOSTICO"
    
    Write-Host "`nTudo configurado! Proximos passos:" -ForegroundColor Green
    Write-Host "
    1. Certifique-se que docker-compose esta rodando:
       docker-compose up -d
    
    2. Execute a aplicacao:
       dotnet run
    
    3. Teste a API:
       curl http://localhost:5001/watch/user1
    
    4. Se houver erro, voce vera sugestoes automaticas!
    " -ForegroundColor White
}

# ===== EXECUCAO PRINCIPAL =====

Write-Host "`nIniciando Diagnostico de Conexao PostgreSQL..." -ForegroundColor Cyan

$dockerOk = Test-DockerRunning
if (-not $dockerOk) {
    Write-Host "`nDocker nao esta rodando. Inicie o Docker Desktop primeiro!" -ForegroundColor Red
    exit 1
}

$containerOk = Test-PostgresContainer
Start-Sleep -Seconds 3

$dbConnOk = Test-DatabaseConnection
if (-not $dbConnOk) {
    Write-Host "`nTentando reiniciar container..." -ForegroundColor Yellow
    docker-compose down
    Start-Sleep -Seconds 5
    docker-compose up -d
    Start-Sleep -Seconds 5
    $dbConnOk = Test-DatabaseConnection
}

if ($dbConnOk) {
    Test-Database
    Show-Logs
}

Show-Configuration

Write-Host "`nDeseja iniciar a aplicacao? (S/N)" -ForegroundColor Yellow
$response = Read-Host

if ($response -eq 'S' -or $response -eq 's') {
    Test-DotnetConnection
}

Show-Summary
