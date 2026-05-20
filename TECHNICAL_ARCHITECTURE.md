# 🏗️ Resumo Técnico da Arquitetura JWT

## Estrutura de Arquivos

```
CameraAccessAPI/
├── src/
│   ├── Infrastructure/Security/
│   │   ├── JwtSettings.cs          ✨ NOVO
│   │   ├── JwtService.cs           🔄 MODIFICADO
│   │   └── AuthenticationExtensions.cs ✨ NOVO
│   │
│   ├── Api/
│   │   ├── Program.cs              🔄 MODIFICADO
│   │   ├── Swagger/                ✨ NOVO (folder)
│   │   └── Controllers/
│   │       ├── AuthController.cs   ✨ NOVO
│   │       └── ... (outros)
│   │
│   └── Tests/Security/
│       └── StreamTokenServiceSecurityTests.cs  🔄 MODIFICADO
│
├── appsettings.json                🔄 MODIFICADO
├── appsettings.Production.json     ✨ NOVO
└── JWT_IMPLEMENTATION_GUIDE.md     ✨ NOVO
```

---

## Detalhes de Cada Componente

### 1. **JwtSettings.cs**

**Responsabilidade:** Configurações fortemente tipadas

```csharp
public class JwtSettings
{
    public string Key { get; set; }                // Chave de 256+ bits
    public string Issuer { get; set; }             // "CameraAccessAPI"
    public string Audience { get; set; }           // "CameraClients"
    public int ExpiryMinutes { get; set; }         // 15-60 minutos
    
    public void ValidateConfiguration()            // Validações obrigatórias
}
```

**Por que:** 
- Type-safe
- Reutilizável em toda aplicação
- Validações centralizadas
- Fácil de testar

---

### 2. **JwtService.cs (Aprimorado)**

**Mudanças:**
```diff
- public JwtService(IConfiguration configuration)
+ public JwtService(IOptions<JwtSettings> options, ILogger<JwtService> logger)

- AddSingleton<JwtService>()
+ AddScoped<JwtService>()  // Melhor para DI

+ Logger para auditoria segura
+ Claims adicionais opcionais
+ Validação obrigatória no construtor
```

**Responsabilidades:**
- Gerar tokens JWT seguros
- Validar configurações
- Logging auditável

---

### 3. **AuthenticationExtensions.cs**

**Responsabilidade:** Configurar todo o pipeline de autenticação

```csharp
public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 1. Registrar JwtSettings e validar
    // 2. Adicionar AddAuthentication()
    // 3. Configurar AddJwtBearer() com TokenValidationParameters
    // 4. Adicionar AddAuthorization() com políticas
}
```

**Validações Implementadas:**
- ✅ ValidateIssuerSigningKey
- ✅ ValidateIssuer
- ✅ ValidateAudience
- ✅ ValidateLifetime
- ✅ ClockSkew = Zero
- ✅ RequireExpirationTime
- ✅ RequireSignedTokens

---

### 4. **AuthController.cs**

**Endpoints:**

| Método | Rota | Requer Auth | Descrição |
|--------|------|-------------|-----------|
| POST | `/api/auth/login` | ❌ Não | Gera JWT com credenciais |
| GET | `/api/auth/validate` | ✅ Sim | Valida token existente |
| GET | `/api/auth/health` | ❌ Não | Health check |

**DTOs:**
```csharp
public class LoginRequest
{
    public string UserId { get; set; }      // Obrigatório
    public string Camera { get; set; }      // Obrigatório
    public string Password { get; set; }    // Obrigatório
}

public class LoginResponse
{
    public string Token { get; set; }       // JWT completo
    public string TokenType { get; set; }   // "Bearer"
    public int ExpiresIn { get; set; }      // Segundos
    public DateTime ExpiresAt { get; set; } // UTC
}
```

---

### 5. **Program.cs (Integração)**

**Ordem Crítica do Pipeline:**

```csharp
// 1. Registrar serviços (ordem não importa)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>();

// 2. Middleware (ORDEM IMPORTA!)
var app = builder.Build();

app.UseHttpsRedirection();              // 1️⃣ HTTPS primeiro
app.UseIpRateLimiting();               // 2️⃣ Rate limit
app.UseMiddleware<ExceptionMiddleware>(); // 3️⃣ Tratamento erro
app.UseAuthentication();                // 4️⃣ AUTENTICAR
app.UseAuthorization();                 // 5️⃣ AUTORIZAR
app.MapControllers();                   // 6️⃣ Rotas
```

**Por que a ordem importa:**
- UseHttpsRedirection ANTES de UseAuthentication
- UseAuthentication ANTES de UseAuthorization
- UseAuthorization ANTES de MapControllers

---

### 6. **Configurações (appsettings.json)**

**Development:**
```json
{
  "Jwt": {
    "Key": "CameraAccessApiLocalDevJwtSecretKey123456789!@#$%^&*()_+-=[]{}|;:",
    "ExpiryMinutes": 15,
    "AllowInsecureHttp": false
  }
}
```

**Production:**
```json
{
  "Jwt": {
    "Key": "${JWT_SECRET_KEY}",  // Variável de ambiente
    "ExpiryMinutes": 30,
    "AllowInsecureHttp": false
  }
}
```

---

## Fluxo Completo de Requisição

```
1. CLIENT SENDS REQUEST
   ├─ POST /api/auth/login
   │  └─ Body: {userId, camera, password}
   │
   └─ OR GET /api/access/validate
      └─ Header: Authorization: Bearer {token}

2. ASP.NET CORE PIPELINE
   ├─ ExceptionMiddleware (trata exceções)
   ├─ IpRateLimitMiddleware (bloqueia abuso)
   ├─ AUTHENTICATION MIDDLEWARE
   │  ├─ Extrai token do header Authorization
   │  ├─ JwtBearerHandler valida:
   │  │  ├─ Assinatura com chave secreta
   │  │  ├─ Issuer (emissor)
   │  │  ├─ Audience (consumidor)
   │  │  ├─ Lifetime (expiração)
   │  │  └─ Claims obrigatórias
   │  └─ Cria ClaimsPrincipal (User)
   │
   └─ AUTHORIZATION MIDDLEWARE
      ├─ Verifica [Authorize] no controller/action
      ├─ Verifica claims necessárias
      └─ Concede/nega acesso

3. CONTROLLER EXECUTA
   ├─ User.Identity?.IsAuthenticated (true/false)
   ├─ User.FindFirst("sub") (userId)
   ├─ User.FindFirst("camera") (camera)
   └─ User.Claims (todas as claims)

4. RESPONSE
   └─ 200 OK ou 401 Unauthorized
```

---

## Segurança em Detalhe

### Hash da Chave JWT

```csharp
string key = "CameraAccessApiLocalDevJwtSecretKey123456789!@#$%^&*()_+-=[]{}|;:";
byte[] keyBytes = Encoding.UTF8.GetBytes(key);
// keyBytes.Length = 64 bytes = 512 bits ✅ (mínimo 32 bytes / 256 bits)
```

### Token JWT Estrutura

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9  ← Header (Base64)
.
eyJzdWIiOiJ1c2VyMTIzIiwiY2FtZXJhIjoiY2FtZXJhLWZyb250LWRvb3IifQ  ← Payload (Base64)
.
_SIGNATURE_CALCULATED_WITH_HMAC_SHA256  ← Assinatura (Base64)
```

**Payload decodificado:**
```json
{
  "sub": "user123",                    // Obrigatório
  "camera": "camera-front-door",       // Obrigatório (domínio)
  "jti": "unique-token-id",            // Obrigatório
  "iat": 1700000000,                   // Obrigatório (issued at)
  "exp": 1700000900,                   // Obrigatório (expires)
  "iss": "CameraAccessAPI",            // Obrigatório (issuer)
  "aud": "CameraClients"               // Obrigatório (audience)
}
```

---

## Injeção de Dependência

```csharp
// Services registrados no Program.cs
builder.Services.AddJwtAuthentication(builder.Configuration);

// Internamente faz:
services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
services.AddScoped<JwtService>();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);
services.AddAuthorization(...);
```

**Injeção em Controller:**
```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        // DI automático pelo ASP.NET Core
    }
}
```

---

## Testes Inclusos

**Arquivo:** `src/Tests/Security/StreamTokenServiceSecurityTests.cs`

**Testes:**
- ✅ Token válido deve ser aceito
- ✅ Token expirado deve ser rejeitado
- ✅ Token alterado deve ser rejeitado
- ✅ Token vazio deve ser rejeitado
- ✅ Token sem claims obrigatórias deve ser rejeitado
- ✅ Token revogado deve ser rejeitado
- ✅ Token com chave diferente deve ser rejeitado

**Executar testes:**
```bash
dotnet test
```

---

## Políticas de Autorização

```csharp
// Registradas em AuthenticationExtensions.cs
options.AddPolicy("AuthenticatedOnly", policy =>
{
    policy.RequireAuthenticatedUser();
});

options.AddPolicy("CameraAccess", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireClaim("camera");
});
```

**Usar em Controller:**
```csharp
[Authorize(Policy = "CameraAccess")]
[HttpPost("access/validate")]
public IActionResult ValidateAccess()
{
    // Requer autenticação + claim "camera"
}
```

---

## Extensibilidade

### Adicionar Novos Claims

```csharp
var token = jwtService.GenerateToken(
    userId: "user123",
    camera: "camera-1",
    additionalClaims: new Dictionary<string, string>
    {
        { "role", "admin" },
        { "department", "security" }
    }
);
```

### Adicionar Novas Políticas

```csharp
options.AddPolicy("AdminOnly", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireClaim("role", "admin");
});

// Usar
[Authorize(Policy = "AdminOnly")]
public IActionResult AdminAction() { }
```

### Adicionar Validação Customizada

```csharp
options.TokenValidationParameters.ValidateTokenReplay = true;
options.Events.OnTokenValidated = async context =>
{
    // Validação customizada
    var tokenId = context.Principal.FindFirst("jti")?.Value;
    var isRevoked = await _tokenBlacklistService.IsRevokedAsync(tokenId);
    if (isRevoked)
    {
        context.Fail("Token has been revoked");
    }
};
```

