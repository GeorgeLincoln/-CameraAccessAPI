# 🚀 GUIA RÁPIDO DE INÍCIO - VALIDAÇÃO

## ⚡ Início Rápido (5 minutos)

### 1️⃣ Compilar o projeto

```bash
cd c:\Users\Jessica\Documents\CameraAccessAPI
dotnet build
```

### 2️⃣ Verificar erros de compilação

```bash
# Deve compilar sem erros
dotnet build --no-restore
```

### 3️⃣ Executar testes

```bash
# Testes unitários de segurança
dotnet test --filter "Security"

# Todos os testes
dotnet test
```

### 4️⃣ Iniciar o projeto

```bash
# Terminal 1: Backend
dotnet run

# Backend estará disponível em:
# http://localhost:5001
# Swagger: http://localhost:5001/swagger
```

---

## 🔍 Verificação de Implementação

### ✅ Verificar que todos os arquivos foram criados

```bash
# DTOs
ls -la src/Application/DTOs/StreamToken*

# Interfaces
ls -la src/Application/Interfaces/IStream*

# Serviços
ls -la src/Infrastructure/Security/StreamTokenService.cs
ls -la src/Application/Services/StreamAccessValidationService.cs

# Controllers (modificados)
ls -la src/Api/Controllers/AccessController.cs
ls -la src/Api/Controllers/WatchController.cs

# Testes
ls -la src/Tests/Security/StreamTokenServiceSecurityTests.cs

# Documentação
ls -la *.md
```

### ✅ Verificar que o código compila

```bash
dotnet build --configuration Release
# Deve resultar em: Build succeeded
```

### ✅ Verificar que os serviços estão registrados

```bash
# Program.cs deve conter:
grep "IStreamTokenService" src/Api/Program.cs
grep "IStreamAccessValidationService" src/Api/Program.cs

# Deve retornar essas linhas
```

### ✅ Verificar configuração de JWT

```bash
# appsettings.json deve ter:
grep -A 5 '"Jwt":' appsettings.json

# Deve mostrar:
#  "ExpiryMinutes": 1
```

### ✅ Verificar configuração MediaMTX

```bash
# mediamtx.yml deve ter:
grep "authHTTPAddress" mediamtx.yml

# Deve mostrar:
# authHTTPAddress: http://host.docker.internal:5001/api/access/stream/validate
```

---

## 🧪 Teste Manual Rápido

### 1️⃣ Obter Token

```bash
# Assumindo userId=test-user e camera=test já existem
curl -s "http://localhost:5001/watch?userId=test-user&camera=test" | jq
```

**Resposta esperada:**
```json
{
  "status": "ok",
  "streamUrl": "http://localhost:8888/test?token=eyJhbGc...",
  "expiresInSeconds": 60,
  "message": "Token válido por 60 segundos"
}
```

### 2️⃣ Validar Token

```bash
# Extrair token da URL acima
TOKEN="eyJhbGc..."

curl -s "http://localhost:5001/api/access/stream/validate?token=$TOKEN&stream=test&ip=127.0.0.1" | jq
```

**Resposta esperada:**
```json
{
  "allowed": true,
  "reason": "Access granted",
  "userId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "cameraId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "processedAt": "2026-05-06T12:34:56.789Z"
}
```

### 3️⃣ Teste com Token Expirado

```bash
# Esperar 65 segundos
sleep 65

# Tentar validar novamente
curl -s "http://localhost:5001/api/access/stream/validate?token=$TOKEN&stream=test&ip=127.0.0.1" | jq
```

**Resposta esperada (401):**
```json
{
  "status": "error",
  "reason": "Token validation failed"
}
```

---

## 📊 Verificação de Logs

### Ver logs de acesso

```bash
# Último arquivo de log
tail -f logs/log-*.txt

# Buscar por validação de token
grep -i "token\|stream access" logs/log-*.txt

# Buscar por acessos negados
grep -i "access denied\|blocked" logs/log-*.txt
```

---

## 🛠️ Troubleshooting

### ❌ Erro: "JWT Key must be at least 256 bits"

**Solução:**
```bash
# Gerar chave válida (32+ caracteres)
openssl rand -base64 32

# Atualizar em appsettings.json
```

### ❌ Erro: "Cannot connect to database"

**Solução:**
```bash
# Verificar conexão PostgreSQL
pg_isready -h localhost -p 5432

# Atualizar connection string
```

### ❌ Erro: "Service not found"

**Solução:**
```bash
# Verificar que serviços estão registrados em Program.cs
grep "AddScoped\|AddSingleton" src/Api/Program.cs

# Recompilar
dotnet clean
dotnet build
```

### ❌ Erro: "Token validation failed"

**Solução:**
```bash
# Verificar logs
tail -f logs/log-*.txt

# Testar token manualmente
# Ir para https://jwt.io
# Colar token
# Verificar claims
```

---

## 📋 Checklist de Validação

Antes de fazer merge, validar:

- [ ] `dotnet build` sem erros
- [ ] `dotnet test` passa todos os testes
- [ ] `dotnet test --filter "Security"` passa testes de segurança
- [ ] GET /watch com parâmetros válidos retorna 200 OK
- [ ] GET /watch sem parâmetros retorna 400 Bad Request
- [ ] POST /api/access/stream/validate com token válido retorna 200 OK
- [ ] POST /api/access/stream/validate com token inválido retorna 401 Unauthorized
- [ ] POST /api/access/stream/validate sem token retorna 401 Unauthorized
- [ ] Logs aparecem corretamente em logs/log-*.txt
- [ ] AccessController e WatchController compila sem warnings
- [ ] StreamTokenService valida JWT corretamente
- [ ] StreamAccessValidationService checa todas as validações

---

## 🎯 Próximos Passos

1. **Code Review**
   - [ ] Revisar DTOs
   - [ ] Revisar Interfaces
   - [ ] Revisar Services
   - [ ] Revisar Controllers
   - [ ] Revisar Testes

2. **Testing em Staging**
   - [ ] Deploy em staging
   - [ ] Executar testes e2e
   - [ ] Monitorar performance
   - [ ] Validar logs

3. **Deploy em Produção**
   - [ ] Backup do banco de dados
   - [ ] Deploy do código
   - [ ] Monitorar métricas
   - [ ] Validar auditoria

---

## 📞 Suporte

**Documentação:**
- [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - Guia completo
- [TESTING_SECURITY.md](TESTING_SECURITY.md) - Testes de segurança
- [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md) - Diagramas
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Resumo

**Testes Manuais:**
- [STREAM_TOKEN_REQUESTS.http](STREAM_TOKEN_REQUESTS.http) - Requisições HTTP

---

**Criado**: 6 de maio de 2026
**Versão**: 1.0.0
**Status**: ✅ Pronto para Teste
