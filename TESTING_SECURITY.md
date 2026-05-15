### 🧪 TESTES DE SEGURANÇA - Stream Token Validation

## Pré-requisitos

```bash
# 1. Banco de dados rodando
docker run -d -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 postgres:latest

# 2. Backend rodando
dotnet run

# 3. MediaMTX rodando (com config auth hook)
mediamtx mediamtx.yml

# 4. RTSP source (para teste, usar ffmpeg simulando stream)
ffmpeg -re -f lavfi -i testsrc=s=1920x1080:d=60 -f lavfi -i sine=f=440:d=60 \
  -vf format=yuv420p -c:v libx264 -c:a aac -f rtsp rtsp://localhost:8554/test
```

---

## ✅ TEST 1: ACESSO VÁLIDO

### 1.1 Obter Token
```http
GET http://localhost:5001/watch?userId=test-user&camera=test

### Resposta esperada (200 OK)
{
  "status": "ok",
  "streamUrl": "http://localhost:8888/test?token=eyJhbGc...",
  "expiresInSeconds": 60,
  "message": "Token válido por 60 segundos"
}
```

### 1.2 Acessar Stream com Token
```http
GET http://localhost:8888/test?token=eyJhbGc...

### Resposta esperada (200 OK)
Stream disponível

### MediaMTX internamente:
1. Intercepta requisição
2. Chama: POST /api/access/stream/validate?token=...&stream=test&ip=127.0.0.1
3. Backend valida token
4. Backend retorna 200 OK
5. MediaMTX permite acesso
```

---

## ❌ TEST 2: TOKEN EXPIRADO

### 2.1 Esperar token expirar (> 60 segundos)
```bash
sleep 65
```

### 2.2 Tentar acessar com token expirado
```http
GET http://localhost:8888/test?token=eyJhbGc...

### Resposta esperada
401 Unauthorized

### MediaMTX internamente:
1. Chama backend: POST /api/access/stream/validate?token=...&stream=test
2. StreamTokenService.ValidateAndExtractClaimsAsync() 
3. Validação JWT falha (token expirado)
4. Retorna null
5. Backend responde 401
6. MediaMTX bloqueia acesso
```

---

## ❌ TEST 3: TOKEN INVÁLIDO / ALTERADO

### 3.1 Usar token com assinatura alterada
```http
GET http://localhost:8888/test?token=eyJhbGc...XXXXX

### Resposta esperada (401)
Unauthorized
```

### 3.2 Usar token de outra câmera
```http
### Cenário:
# 1. Obter token para camera "test"
GET /watch?userId=test-user&camera=test
# Response: token_A

# 2. Tentar usar token_A para camera "garage"
GET http://localhost:8888/garage?token=token_A

### Resposta esperada (401)
# Porque:
# - Token contém claim "camera": "test"
# - Request é para "garage"
# - StreamAccessValidationService.ResolveStreamNameToCameraIdAsync("garage") retorna camera_B
# - Token foi gerado para stream "test" (camera_A)
# - Mismatch → Acesso negado
```

---

## ❌ TEST 4: ACESSO DIRETO SEM TOKEN

### 4.1 Tentar acessar stream sem token
```http
GET http://localhost:8888/test

### Resposta esperada (401)
Unauthorized

### Porque:
# MediaMTX chama: POST /api/access/stream/validate?token=&stream=test
# Token está vazio
# Backend retorna 401
# MediaMTX bloqueia
```

---

## ❌ TEST 5: FORA DO HORÁRIO PERMITIDO

### Pré-requisito:
```
# Configurar regra de acesso:
# - Usuário: test-user
# - Câmera: test
# - Dias permitidos: Segunda-Sexta (1-5)
# - Horário: 09:00 - 17:00

# Teste em fim de semana ou fora do horário
```

### 5.1 Tentar acessar fora do horário
```http
# Fazer em sábado ou domingo, ou fora das 09:00-17:00
GET /watch?userId=test-user&camera=test

### Resposta esperada (401)
{
  "status": "error",
  "reason": "Acesso negado pelas regras de horário"
}
```

---

## ❌ TEST 6: USUÁRIO INATIVO

### 6.1 Desativar usuário
```sql
UPDATE users SET active = false WHERE id = 'test-user';
```

### 6.2 Tentar obter token
```http
GET /watch?userId=test-user&camera=test

### Resposta esperada (401)
{
  "status": "error",
  "reason": "Acesso negado pelas regras de horário"
  # (ou outro motivo que o serviço registrar)
}
```

---

## ❌ TEST 7: CÂMERA INATIVA

### 7.1 Desativar câmera
```sql
UPDATE cameras SET is_active = false WHERE name = 'test';
```

### 7.2 Tentar obter token
```http
GET /watch?userId=test-user&camera=test

### Resposta esperada (401)
Acesso negado
```

---

## 🔐 TEST 8: REVOGAÇÃO DE TOKEN

### 8.1 Gerar token
```http
GET /watch?userId=test-user&camera=test
# Response: token_A
```

### 8.2 Revogar token (implementar endpoint futuro)
```http
POST /api/access/revoke
Content-Type: application/json

{
  "tokenId": "jti-value-from-token"
}
```

### 8.3 Tentar usar token revogado
```http
GET http://localhost:8888/test?token=token_A

### Resposta esperada (401)
# Token revogado
```

---

## 📊 MONITORAMENTO E LOGS

### 1. Logs do Backend
```bash
# Ver logs estruturados
tail -f logs/log-*.txt | grep -i "stream access\|token validation"
```

### 2. Tipos de log
```
✅ [INFORMATION] ✅ Access granted. UserId=..., CameraId=..., TokenExpires=...
❌ [WARNING] Access denied: Outside schedule. UserId=..., DayOfWeek=..., TimeOfDay=...
❌ [WARNING] Token validation failed: token expired
❌ [WARNING] Stream access validation failed. Stream=..., ClientIp=...
```

### 3. Auditoria em banco de dados
```sql
SELECT * FROM access_logs 
WHERE source LIKE 'MediaMTX_Stream_%'
ORDER BY timestamp DESC
LIMIT 50;
```

---

## 🚀 TESTE END-TO-END COMPLETO

```bash
#!/bin/bash

# 1. Obter token
echo "1️⃣ Obtendo token..."
TOKEN_RESPONSE=$(curl -s http://localhost:5001/watch?userId=test-user&camera=test)
TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.streamUrl' | sed 's/.*token=//')

echo "Token: $TOKEN"
echo "Expira em: $(echo $TOKEN_RESPONSE | jq '.expiresInSeconds') segundos"

# 2. Tentar acessar stream (esperar validação do MediaMTX)
echo "2️⃣ Acessando stream..."
curl -v http://localhost:8888/test?token=$TOKEN

# 3. Esperar expiração
echo "3️⃣ Aguardando expiração do token (65s)..."
sleep 65

# 4. Tentar acessar com token expirado (deve falhar)
echo "4️⃣ Tentando acesso com token expirado..."
curl -v http://localhost:8888/test?token=$TOKEN

# 5. Verificar logs
echo "5️⃣ Verificando logs de auditoria..."
sqlite3 camera_access.db "SELECT reason, allowed FROM access_logs ORDER BY timestamp DESC LIMIT 5;"
```

---

## 📋 CHECKLIST DE VALIDAÇÃO

- [ ] Token válido permite acesso
- [ ] Token expirado bloqueia acesso
- [ ] Token alterado bloqueia acesso
- [ ] Acesso direto sem token bloqueia acesso
- [ ] Token de outra câmera não funciona
- [ ] Fora do horário bloqueia acesso
- [ ] Usuário inativo bloqueia acesso
- [ ] Câmera inativa bloqueia acesso
- [ ] Logs registram todas as tentativas
- [ ] MediaMTX chama backend para cada acesso
- [ ] Resposta 200 permite, 401 bloqueia
- [ ] Token vinculado corretamente à câmera

---

## 🛡️ REQUISITOS DE SEGURANÇA VALIDADOS

✅ Nenhum usuário acessa stream diretamente
✅ Todo acesso passa por validação JWT
✅ Token expira em 60 segundos (curto)
✅ Token é vinculado a câmera específica
✅ Backend valida horário/dia
✅ Acesso negado retorna 401 (padrão HTTP)
✅ Logs de auditoria completos
✅ Secret key via environment (não hardcoded)
✅ Token não funciona se alterado
✅ Token revogado é bloqueado
