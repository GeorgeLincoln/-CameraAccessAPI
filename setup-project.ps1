# Setup Completo do Projeto CameraAccessAPI com pgAdmin

# Cores para output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Error-Custom { Write-Host $args -ForegroundColor Red }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning-Custom { Write-Host $args -ForegroundColor Yellow }

Clear-Host
Write-Info "╔════════════════════════════════════════════════════════════════╗"
Write-Info "║     Setup CameraAccessAPI + PostgreSQL + pgAdmin              ║"
Write-Info "╚════════════════════════════════════════════════════════════════╝"

# 1. Verificar Docker
Write-Info "`nVerificando Docker..."
try {
    $dockerVersion = docker --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Docker encontrado: $dockerVersion"
    }
} catch {
    Write-Error-Custom "Docker nao esta instalado ou nao respondeu"
    Write-Error-Custom "   Instale do: https://www.docker.com/products/docker-desktop"
    exit 1
}

# 2. Verificar Docker Desktop rodando
Write-Info "`nVerificando Docker Desktop..."
$dockerPs = docker ps 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Docker Desktop nao esta rodando"
    Write-Warning-Custom "   Inicie o Docker Desktop e tente novamente"
    exit 1
}
Write-Success "Docker Desktop esta rodando"

# 3. Para containers existentes
Write-Info "`nParando containers antigos (se existirem)..."
docker-compose down 2>&1 | Out-Null
Start-Sleep -Seconds 2

# 4. Iniciar docker-compose
Write-Info "`nIniciando docker-compose..."
Write-Warning-Custom "   Aguarde... (PostgreSQL + pgAdmin)"
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Erro ao iniciar docker-compose"
    docker-compose logs
    exit 1
}

Write-Success "Docker-compose iniciado"

# 5. Aguardar PostgreSQL ficar healthy
Write-Info "`nAguardando PostgreSQL ficar pronto..."
$maxRetries = 30
$retries = 0

while ($retries -lt $maxRetries) {
    $status = docker exec camera_access_db pg_isready -U postgres 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "PostgreSQL pronto!"
        break
    }
    Write-Warning-Custom "   Tentativa $($retries+1)/$maxRetries..."
    Start-Sleep -Seconds 2
    $retries++
}

if ($retries -eq $maxRetries) {
    Write-Error-Custom "PostgreSQL nao ficou pronto a tempo"
    docker-compose logs postgres
    exit 1
}

# 6. Verificar banco de dados
Write-Info "`nVerificando banco de dados..."
$dbCheck = docker exec camera_access_db psql -U postgres -d CameraAccessDb -c 'SELECT 1;' 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Success 'Banco de dados criado e acessivel'
} else {
    Write-Error-Custom 'Banco de dados pode nao estar inicializado'
}

# 7. Verificar tabelas
Write-Info "`nVerificando tabelas..."
$tableCheck = docker exec camera_access_db psql -U postgres -d CameraAccessDb -c 'SELECT COUNT(*) FROM "AccessRules";' 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Success "Tabela AccessRules existe"
} else {
    Write-Warning-Custom "Tabela pode nao estar inicializada"
}

# 8. Resumo de acesso
Write-Success "`nSetup Completo!"
Write-Info "`n╔════════════════════════════════════════════════════════════════╗"
Write-Info "║              Servicos Disponiveis                              ║"
Write-Info "╚════════════════════════════════════════════════════════════════╝"

Write-Info "`npgAdmin (Gerenciador do Banco):"
Write-Host "   URL:       http://localhost:5050"
Write-Host "   Email:     admin@admin.com"
Write-Host "   Senha:     admin123"

Write-Info "`nPostgreSQL:"
Write-Host "   Host:      localhost"
Write-Host "   Port:      5432"
Write-Host "   User:      postgres"
Write-Host "   Password:  yourpassword"
Write-Host "   Database:  CameraAccessDb"

Write-Info "`nMediaMTX:"
Write-Host "   URL:       http://localhost:8888"

Write-Info "`nAPI (Proximo passo):"
Write-Host "   Comando:   dotnet run"
Write-Host "   URL:       http://localhost:5001"

# 9. Perguntar se deseja abrir pgAdmin
Write-Info "`nDeseja abrir pgAdmin no navegador? (S/N)"
$response = Read-Host
if ($response -eq 'S' -or $response -eq 's') {
    Write-Info "Abrindo pgAdmin..."
    Start-Process "http://localhost:5050"
}

# 10. Perguntar se deseja iniciar API
Write-Info "`nDeseja compilar e executar a API agora? (S/N)"
$response = Read-Host
if ($response -eq 'S' -or $response -eq 's') {
    Write-Info "`nCompilando projeto..."
    dotnet build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build bem-sucedido!"
        Write-Info "Iniciando API..."
        dotnet run
    } else {
        Write-Error-Custom "Erro na compilacao"
        Write-Error-Custom "   Tente: dotnet clean; dotnet restore; dotnet build"
    }
} else {
    Write-Info "`nPara iniciar a API depois, execute:"
    Write-Host "   dotnet run"
}

Write-Info "`nPronto para usar! Abra pgAdmin em http://localhost:5050"
