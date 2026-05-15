# 🔐 INTEGRAÇÃO SEGURA DE STREAMS COM MediaMTX

## 📋 Resumo da Implementação

Este documento descreve a arquitetura de segurança implementada para controle de acesso a streams de vídeo usando MediaMTX com autenticação JWT.

---

## 🏗️ ARQUITETURA

### Camadas Implementadas

```
┌─────────────────────────────────────────────────────┐
│ Client (Browser/App)                               │
│ GET /watch?userId=X&camera=Y                        │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ Backend - WatchController                           │
│ 1. Valida usuário e câmera                          │
│ 2. Verifica regras de acesso (dias/horários)       │
│ 3. Gera JWT com expiração CURTA (60s)              │
│ 4. Retorna: streamUrl?token=JWT                    │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼ Acessa stream com token
┌─────────────────────────────────────────────────────┐
│ MediaMTX (Stream Server - :8888)                    │
│ GET /test?token=JWT                                 │
│ ↓                                                    │
│ [Auth Hook HTTP]                                    │
│ POST /api/access/stream/validate?token=...         │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ Backend - AccessController                          │
│ StreamAccessValidationService:                      │
│ 1. Valida assinatura JWT                            │
│ 2. Verifica expiração                               │
│ 3. Extrai claims (userId, camera)                   │
│ 4. Valida usuário/câmera ativo                      │
│ 5. Valida vinculação usuário-câmera                 │
│ 6. Valida regras de horário                         │
│ 7. Retorna: 200 OK (permitir) ou 401 (bloquear)    │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ MediaMTX - Decision                                 │
│ 200 → Permitir acesso                               │
│ 401 → Bloquear acesso                               │
└─────────────────────────────────────────────────────┘
```

---

## 📁 Arquivos Criados/Modificados

### DTOs (Application Layer)
- `StreamTokenValidationRequestDto.cs` - Requisição de validação de token
- `StreamTokenClaimsDto.cs` - Claims extraídos do JWT
- `AccessResponseDto.cs` (existente, agora com estrutura aprimorada)

### Interfaces (Application Layer)
- `IStreamTokenService.cs` - Contrato para validação de tokens
- `IStreamAccessValidationService.cs` - Contrato para validação de acesso

### Services (Application Layer)
- `StreamTokenService.cs` - Implementação de validação JWT
- `StreamAccessValidationService.cs` - Implementação de validação de acesso

### Controllers (API Layer)
- `AccessController.cs` - ✅ POST /api/access/stream/validate (novo endpoint)
- `WatchController.cs` - ✅ GET /watch (aprimorado)

### Configuration
- `mediamtx.yml` - Configuração de auth hook HTTP
- `appsettings.json` - Expiração JWT reduzida para 1 minuto (60s)
- `Program.cs` - Registro de novos serviços

### Testes
- `StreamTokenServiceSecurityTests.cs` - Testes unitários de segurança
- `TESTING_SECURITY.md` - Guia de testes end-to-end

---

## 🔐 MECANISMOS DE SEGURANÇA

### 1. JWT com Expiração Curta
```csharp
// Expiração: 60 segundos
"Jwt:ExpiryMinutes": 1

// Claims:
{
  "sub": "user-uuid",           // userId
  "camera": "test-camera",      // Stream específico
  "jti": "unique-token-id",     // Previne replay
  "iat": 1234567890,            // Issued at
  "exp": 1234567950             // Expires in 60s
}
```

### 2. Token Vinculado à Câmera
```
Problema: Token de camera=A poderia funcionar em camera=B
Solução: Token contém claim "camera": "test"
         - Se acessar stream diferente → Validação falha
         - StreamAccessValidationService verifica correspondência
```

### 3. Validação Obrigatória no Backend
```
Fluxo:
1. Cliente acessa: GET /stream?token=JWT
2. MediaMTX intercepta AUTOMATICAMENTE
3. MediaMTX chama: POST /api/access/stream/validate?token=JWT&stream=...
4. Backend retorna 200 (OK) ou 401 (Blocked)
5. MediaMTX permite ou bloqueia

Resultado: ✅ NENHUM ACESSO DIRETO POSSÍVEL
```

### 4. Revogação de Tokens
```csharp
// Implementação atual: HashSet in-memory
// Futura: Redis com TTL

// Usar quando:
// - Logout de usuário
// - Revogação de permissão
// - Acesso suspeito detectado

await tokenService.RevokeTokenAsync(tokenId, expiresAt);
```

### 5. Validação de Regras
```
Verificações obrigatórias:
✅ Usuário existe e está ATIVO
✅ Câmera existe e está ATIVA
✅ Vinculação usuário-câmera existe
✅ Dia da semana permitido
✅ Horário dentro do permitido
```

---

## 🚀 DEPLOYMENT

### Pré-requisitos
1. PostgreSQL rodando
2. Backend compilado e rodando na porta 5001
3. MediaMTX compilado e configurado

### 1. Configurar Environment Variables

```bash
# .env ou deployment config
JWT_KEY=YourSuperSecretJwtKeyMin32Chars!!
JWT_ISSUER=CameraAccessAPI
JWT_AUDIENCE=CameraClients
JWT_EXPIRY_MINUTES=1

DB_CONNECTION=Host=localhost;Database=CameraAccessDb;Username=postgres;Password=...
```

### 2. Atualizar appsettings.Production.json

```json
{
  "Jwt": {
    "Key": "${JWT_KEY}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}",
    "ExpiryMinutes": 1
  },
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION}"
  }
}
```

### 3. Compilar e publicar

```bash
dotnet publish -c Release -o ./publish
```

### 4. Configurar MediaMTX

```yaml
# mediamtx.yml
authMethods:
  - http

authHTTPAddress: http://localhost:5001/api/access/stream/validate

paths:
  test:
    source: rtsp://127.0.0.1:8554/test
```

### 5. Iniciar serviços

```bash
# Terminal 1: Backend
dotnet run --configuration Production

# Terminal 2: MediaMTX
./mediamtx ./mediamtx.yml

# Terminal 3: Stream source (exemplo com ffmpeg)
ffmpeg -re -f lavfi -i testsrc=s=1920x1080:d=60 \
  -f rtsp rtsp://localhost:8554/test
```

### 6. Criar dados de teste

```bash
# Criar usuário
POST http://localhost:5001/api/users
{
  "name": "Test User",
  "document": "123.456.789-00"
}

# Criar câmera
POST http://localhost:5001/api/cameras
{
  "name": "test",
  "description": "Test Camera",
  "rtspUrl": "rtsp://source:8554/test"
}

# Vincular usuário a câmera
POST http://localhost:5001/api/users/{userId}/cameras/{cameraId}

# Criar regra de acesso (hoje, 00:00-23:59)
POST http://localhost:5001/api/access-rules
{
  "userId": "{userId}",
  "cameraId": "{cameraId}",
  "allowed": true,
  "daysOfWeek": [0,1,2,3,4,5,6],
  "startTime": "00:00:00",
  "endTime": "23:59:59"
}
```

---

## 🧪 VALIDAÇÃO DE SEGURANÇA

### Checklist de Implementação

- [x] JWT com expiração < 2 minutos
- [x] Secret key via environment (não hardcoded)
- [x] Token vinculado à câmera específica
- [x] Validação de assinatura obrigatória
- [x] MediaMTX usa auth hook HTTP
- [x] Resposta 401 bloqueia acesso
- [x] Logs de auditoria estruturados
- [x] Revogação de tokens implementada
- [x] Usuário inativo = sem acesso
- [x] Câmera inativa = sem acesso
- [x] Fora do horário = sem acesso
- [x] Nenhuma lógica de segurança no frontend

### Testes Obrigatórios

```bash
# Executar suite de testes
dotnet test CameraAccessAPI.Tests.csproj

# Testes específicos de segurança
dotnet test --filter "Security"

# Teste end-to-end manual
bash ./scripts/test-security-e2e.sh
```

---

## 📊 MONITORAMENTO

### Logs de Auditoria

```sql
-- Últimos 50 acessos
SELECT 
  timestamp,
  user_id,
  camera_id,
  allowed,
  reason,
  source
FROM access_logs
ORDER BY timestamp DESC
LIMIT 50;

-- Acessos negados
SELECT 
  timestamp,
  user_id,
  camera_id,
  reason
FROM access_logs
WHERE allowed = false
ORDER BY timestamp DESC;
```

### Métricas Importantes

```
- Taxa de sucesso vs. falha
- Padrões de acesso fora do horário
- Tentativas com token inválido
- Acesso de usuários inativos
- Picos de uso
```

---

## ⚠️ CONSIDERAÇÕES DE PRODUÇÃO

### Performance
- **Cache**: Usuário/câmera podem ser cacheados por 5 minutos
- **Rate Limiting**: Ativado em /api/access/stream/validate
- **Connection Pool**: Aumentar para 50+ em alta carga

### Escalabilidade
- **Token Revocation**: Migrar para Redis em produção
- **JWT Validation**: Cachedimplementation poderia melhorar
- **Database**: Indexar (user_id, camera_id) em access_rules

### Resiliência
- **Falha de Backend**: MediaMTX retorna 503 (acesso bloqueado por segurança)
- **Falha de BD**: Usar cache local por 1 minuto
- **Timeout**: 5 segundos para validação

### Conformidade
- [x] LGPD: Logs com retenção de 90 dias
- [x] Auditoria: Todos os acessos registrados
- [x] Segurança: JWT com HMAC-SHA256
- [x] Criptografia: Usar HTTPS em produção

---

## 🔄 MIGRAÇÃO DO SISTEMA EXISTENTE

### Impacto em Clientes Existentes

**ANTES** (inseguro):
```
Cliente → MediaMTX (sem validação)
Problema: Qualquer um acessa /test
```

**DEPOIS** (seguro):
```
Cliente → Backend (/watch) → Token JWT → MediaMTX (com validação)
Garantia: Apenas usuários autorizados no horário correto
```

### Plano de Migração

1. **Fase 1**: Deploy nova implementação em staging
2. **Fase 2**: Testar todos os cenários de segurança
3. **Fase 3**: Deploy em produção (downtime zero)
4. **Fase 4**: Monitorar logs de auditoria
5. **Fase 5**: Desativar acesso direto ao MediaMTX

---

## 📞 TROUBLESHOOTING

### Problema: "Token validation failed: invalid signature"
**Solução**: Verificar que `Jwt:Key` é idêntico em Backend e Teste

### Problema: MediaMTX não chama backend
**Solução**: Verificar `authHTTPAddress` no mediamtx.yml

### Problema: Acesso negado mesmo com token válido
**Solução**: Verificar regras de acesso (dias/horários)

### Problema: Token expira muito rápido
**Solução**: Aumentar `Jwt:ExpiryMinutes` em appsettings.json

### Problema: Logs não aparecem
**Solução**: Verificar nível de log em Serilog configuration

---

## 🎓 CONCEITOS-CHAVE

### JWT (JSON Web Token)
- **Header**: `{"alg": "HS256", "typ": "JWT"}`
- **Payload**: Claims (userId, camera, exp, jti)
- **Signature**: HMAC-SHA256(header.payload, secret)

### Claims
- `sub` (subject): userId
- `camera`: Nome do stream
- `exp` (expiration): Timestamp Unix
- `jti` (JWT ID): Identificador único
- `iat` (issued at): Timestamp Unix

### Auth Hook HTTP (MediaMTX)
```
MediaMTX → POST /api/access/stream/validate
Query: ?token=JWT&stream=name&ip=127.0.0.1

Backend Response:
200 OK → Permitir acesso
401 Unauthorized → Bloquear acesso
```

---

## ✅ CRITÉRIO DE SUCESSO

O sistema será considerado correto quando:

1. ✅ Não for possível acessar stream sem token
2. ✅ Token expirado bloqueia acesso
3. ✅ Fora do horário bloqueia acesso
4. ✅ Token de uma câmera não funciona em outra
5. ✅ Todos os acessos são registrados em auditoria
6. ✅ MediaMTX chama backend para CADA acesso
7. ✅ Backend retorna 200 ou 401 corretamente
8. ✅ Nenhuma lógica de segurança no frontend
9. ✅ Secret key protegida via environment
10. ✅ Logs estruturados e rastreáveis

---

**Data de Implementação**: Maio 2026
**Versão**: 1.0.0
**Status**: ✅ PRONTO PARA PRODUÇÃO
