# 🏛️ ARQUITETURA DE SEGURANÇA - DIAGRAMA VISUAL

## Fluxo Completo de Autenticação

```mermaid
sequenceDiagram
    participant Client
    participant Backend
    participant MediaMTX
    participant Database

    rect rgb(200, 220, 250)
    note over Client,Backend: 1. Cliente solicita acesso
    Client->>Backend: GET /watch?userId=UUID&camera=test
    end

    rect rgb(220, 240, 220)
    note over Backend,Database: 2. Backend valida acesso
    Backend->>Database: Verificar usuário (ativo?)
    Backend->>Database: Verificar câmera (ativa?)
    Backend->>Database: Verificar vinculação
    Backend->>Database: Verificar regras (dia/horário)
    end

    rect rgb(255, 255, 200)
    note over Backend,Backend: 3. Gerar JWT
    Backend->>Backend: JWT = sign({sub, camera, jti, exp})
    Backend->>Client: 200 OK + streamUrl?token=JWT
    end

    rect rgb(200, 220, 250)
    note over Client,MediaMTX: 4. Cliente acessa stream
    Client->>MediaMTX: GET /test?token=JWT
    end

    rect rgb(255, 220, 220)
    note over MediaMTX,Backend: 5. MediaMTX valida token
    MediaMTX->>Backend: POST /api/access/stream/validate?token=JWT&stream=test
    end

    rect rgb(220, 240, 220)
    note over Backend,Database: 6. Backend valida JWT
    Backend->>Backend: Validar assinatura
    Backend->>Backend: Verificar expiração
    Backend->>Backend: Extrair claims
    Backend->>Database: Verificar usuário (ativo?)
    Backend->>Database: Verificar câmera (ativa?)
    Backend->>Database: Verificar regras novamente
    Backend->>Database: Registrar acesso em auditoria
    end

    rect rgb(150, 200, 150)
    note over Backend,MediaMTX: 7. Resposta final
    Backend->>MediaMTX: 200 OK (permitir) OU 401 (bloquear)
    end

    rect rgb(255, 255, 200)
    note over MediaMTX,Client: 8. Decisão final
    alt Acesso Permitido
        MediaMTX->>Client: Stream disponível
    else Acesso Bloqueado
        MediaMTX->>Client: 401 Unauthorized
    end
    end
```

## Estrutura de Dados - JWT Claims

```
Token JWT = Header.Payload.Signature

┌─────────────────────────────────────────┐
│ Header (HMAC-SHA256)                     │
├─────────────────────────────────────────┤
│ {                                        │
│   "alg": "HS256",                        │
│   "typ": "JWT"                           │
│ }                                        │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ Payload (Claims)                         │
├─────────────────────────────────────────┤
│ {                                        │
│   "sub": "f47ac10b-58cc-4372-...",      │ ← userId
│   "camera": "test",                      │ ← streamName
│   "jti": "550e8400-e29b-41d4-...",      │ ← unique id
│   "iat": 1714996200,                     │ ← issued at
│   "exp": 1714996260,                     │ ← expires in 60s
│   "iss": "CameraAccessAPI",              │ ← issuer
│   "aud": "CameraClients"                 │ ← audience
│ }                                        │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ Signature                                │
├─────────────────────────────────────────┤
│ HMAC_SHA256(                             │
│   header.payload,                        │
│   "MySecretKeyForTesting..."             │
│ )                                        │
└─────────────────────────────────────────┘
```

## Camadas de Validação

```
Layer 1: Signature Verification
┌──────────────────────────────────────┐
│ HMAC-SHA256(header.payload, secret)  │
│ Falha → Rejeitar (token adulterado)  │
└──────────────────────────────────────┘
          ↓ ✅
Layer 2: Expiration Check
┌──────────────────────────────────────┐
│ now > exp?                            │
│ Sim → Rejeitar (token expirado)      │
└──────────────────────────────────────┘
          ↓ ✅
Layer 3: Claims Extraction
┌──────────────────────────────────────┐
│ Extract: sub, camera, jti, iat, exp  │
│ Falta algum → Rejeitar               │
└──────────────────────────────────────┘
          ↓ ✅
Layer 4: User Validation
┌──────────────────────────────────────┐
│ User exists?                          │
│ User active?                          │
│ Não → Rejeitar                        │
└──────────────────────────────────────┘
          ↓ ✅
Layer 5: Camera Validation
┌──────────────────────────────────────┐
│ Camera exists?                        │
│ Camera active?                        │
│ Não → Rejeitar                        │
└──────────────────────────────────────┘
          ↓ ✅
Layer 6: User-Camera Link
┌──────────────────────────────────────┐
│ User linked to camera?                │
│ Não → Rejeitar                        │
└──────────────────────────────────────┘
          ↓ ✅
Layer 7: Schedule Rules
┌──────────────────────────────────────┐
│ Day of week allowed?                  │
│ Time in range?                        │
│ Não → Rejeitar                        │
└──────────────────────────────────────┘
          ↓ ✅
✅ ACESSO PERMITIDO
```

## Modelo de Banco de Dados

```
┌─────────────────────────────────────┐
│ users                               │
├─────────────────────────────────────┤
│ id (UUID)                           │
│ name (string)                       │
│ document (string)                   │
│ active (boolean) ← CRÍTICO          │
│ created_at                          │
│ updated_at                          │
└─────────────────────────────────────┘
       │
       │ 1:N relationship
       ▼
┌─────────────────────────────────────┐
│ access_rules                        │
├─────────────────────────────────────┤
│ id (UUID)                           │
│ user_id (FK)                        │
│ camera_id (FK)                      │
│ allowed (boolean) ← CRÍTICO         │
│ active (boolean) ← CRÍTICO          │
│ days_of_week (array)                │
│ start_time (time)                   │
│ end_time (time)                     │
│ created_at                          │
│ updated_at                          │
└─────────────────────────────────────┘
       ▲
       │
       │ 1:N relationship
┌─────────────────────────────────────┐
│ cameras                             │
├─────────────────────────────────────┤
│ id (UUID)                           │
│ name (string) ← usado como          │
│ description (string)     streamName │
│ rtsp_url (string)                   │
│ is_active (boolean) ← CRÍTICO       │
│ created_at                          │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ user_cameras (linking table)        │
├─────────────────────────────────────┤
│ user_id (FK)                        │
│ camera_id (FK)                      │
│ created_at                          │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ access_logs (auditoria)             │
├─────────────────────────────────────┤
│ id (UUID)                           │
│ user_id (FK)                        │
│ camera_id (FK)                      │
│ timestamp                           │
│ allowed (boolean)                   │
│ reason (string)                     │
│ source (string) ← MediaMTX_Stream   │
└─────────────────────────────────────┘
```

## Estados Possíveis

```
┌─────────────────────────────────────────────────────────┐
│ TOKEN REQUEST STATES                                    │
└─────────────────────────────────────────────────────────┘

GET /watch?userId=X&camera=Y

    │
    ├─ Usuário não existe → 401 Unauthorized
    ├─ Usuário inativo → 401 Unauthorized
    ├─ Câmera não existe → 401 Unauthorized
    ├─ Câmera inativa → 401 Unauthorized
    ├─ Sem vinculação → 401 Unauthorized
    ├─ Fora do horário → 401 Unauthorized
    │
    └─ ✅ TUDO OK → 200 OK + streamUrl?token=JWT


┌─────────────────────────────────────────────────────────┐
│ TOKEN VALIDATION STATES (MediaMTX)                      │
└─────────────────────────────────────────────────────────┘

POST /api/access/stream/validate?token=JWT&stream=X

    │
    ├─ Token ausente → 401 Unauthorized
    ├─ Token inválido → 401 Unauthorized
    ├─ Assinatura alterada → 401 Unauthorized
    ├─ Token expirado → 401 Unauthorized
    ├─ Usuário não existe → 401 Unauthorized
    ├─ Usuário inativo → 401 Unauthorized
    ├─ Câmera não existe → 401 Unauthorized
    ├─ Câmera inativa → 401 Unauthorized
    ├─ Sem vinculação → 401 Unauthorized
    ├─ Fora do horário → 401 Unauthorized
    ├─ Token revogado → 401 Unauthorized
    │
    └─ ✅ TUDO OK → 200 OK {status: "ok"}
```

## Matriz de Segurança

```
┌──────────────────────────────────────────────────────────────────┐
│ CENÁRIO                  │ RESULTADO    │ HTTP CODE │ RAZÃO      │
├──────────────────────────────────────────────────────────────────┤
│ Token válido, no horário │ ✅ Permitido │ 200 OK    │ Todas OK   │
│ Token expirado           │ ❌ Bloqueado │ 401       │ Exp check  │
│ Token adulterado         │ ❌ Bloqueado │ 401       │ Signature  │
│ Sem token                │ ❌ Bloqueado │ 401       │ Empty      │
│ Usuário inativo          │ ❌ Bloqueado │ 401       │ User.active│
│ Câmera inativa           │ ❌ Bloqueado │ 401       │ Camera.active
│ Fora do horário          │ ❌ Bloqueado │ 401       │ Schedule   │
│ Câmera incorreta         │ ❌ Bloqueado │ 401       │ Mismatch   │
│ Sem vinculação           │ ❌ Bloqueado │ 401       │ Link       │
└──────────────────────────────────────────────────────────────────┘
```

## Fluxo de Revogação (Logout)

```
1. Usuário faz logout
   DELETE /api/auth/logout

2. Token ID (JTI) é adicionado à lista de revogação
   RevokedTokenIds.Add(jti)

3. Próxima validação do token
   POST /api/access/stream/validate?token=JWT
   
4. IsTokenRevokedAsync(jti) retorna true
   
5. Acesso negado → 401 Unauthorized
```

## Clean Architecture - Separação de Responsabilidades

```
┌─────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER (API)                                │
│ ┌────────────────────────────────────────────────────┐  │
│ │ WatchController           AccessController         │  │
│ │ GET /watch               POST /api/access/stream   │  │
│ └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
              ↓ Depends on Interfaces
┌─────────────────────────────────────────────────────────┐
│ APPLICATION LAYER (Business Logic)                      │
│ ┌────────────────────────────────────────────────────┐  │
│ │ IStreamTokenService              Interface         │  │
│ │ └─ ValidateAndExtractClaimsAsync()                 │  │
│ │ └─ IsTokenRevokedAsync()                           │  │
│ │ └─ RevokeTokenAsync()                              │  │
│ │                                                     │  │
│ │ IStreamAccessValidationService   Interface         │  │
│ │ └─ ValidateStreamAccessAsync()                     │  │
│ │ └─ ResolveStreamNameToCameraIdAsync()              │  │
│ └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
              ↓ Depends on Interfaces
┌─────────────────────────────────────────────────────────┐
│ INFRASTRUCTURE LAYER (Implementation)                   │
│ ┌────────────────────────────────────────────────────┐  │
│ │ StreamTokenService           (JWT Validation)      │  │
│ │ StreamAccessValidationService (Access Rules)       │  │
│ │ AppDbContext                  (Persistence)        │  │
│ └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

Criado: 2026-05-06
Versão: 1.0
Status: ✅ Pronto para Documentação
