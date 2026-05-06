# 🏗️ Arquitetura do Projeto - Fluxo Completo

## 📊 Diagrama de Componentes

```
╔════════════════════════════════════════════════════════════════════════════╗
║                         CameraAccessAPI - Arquitetura                      ║
╚════════════════════════════════════════════════════════════════════════════╝

┌──────────────────────────────────────────────────────────────────────────┐
│ CLIENT LAYER (Camada de Cliente)                                        │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌──────────────┐  ┌──────────────────┐            │
│  │  curl / Postman │  │  Navegador   │  │  MediaMTX Client │            │
│  └────────┬────────┘  └──────┬───────┘  └────────┬─────────┘            │
│           │                  │                   │                      │
│           └──────────────────┼───────────────────┘                      │
│                              │                                          │
└──────────────────────────────┼──────────────────────────────────────────┘
                               │ HTTP/REST
                               ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ API LAYER (Camada de API) - .NET 10 / ASP.NET Core                      │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      Program.cs (Entry Point)                      │ │
│  │  ┌─ Serilog (Logging)                                             │ │
│  │  ├─ Retry Policy (5 tentativas)                                   │ │
│  │  ├─ Health Check (Validar conexão)                                │ │
│  │  └─ Middleware Pipeline                                          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                          │                                              │
│  ┌────────────────────────┼────────────────────────────────────────────┐ │
│  │            Exception Middleware                                     │ │
│  │  (Detecta erro 28P01, oferece sugestões automáticas)               │ │
│  └────────────────────────┼────────────────────────────────────────────┘ │
│                          │                                              │
│  ┌────────────────────────┼────────────────────────────────────────────┐ │
│  │      Rate Limiting Middleware                                      │ │
│  │  (10 requisições por minuto por IP)                               │ │
│  └────────────────────────┼────────────────────────────────────────────┘ │
│                          │                                              │
│  ┌────────────────────────▼────────────────────────────────────────────┐ │
│  │              Controllers (Presentation Layer)                       │ │
│  │  ┌──────────────────────────────────────────────────────────────┐ │ │
│  │  │  WatchController                                            │ │ │
│  │  │    GET /watch/{userId}                                      │ │ │
│  │  │      └─ Chama AccessService.HasAccessAsync()               │ │ │
│  │  └──────────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────┬────────────────────────────────────────────┘ │
│                           │                                             │
└───────────────────────────┼─────────────────────────────────────────────┘
                            │
┌───────────────────────────┼─────────────────────────────────────────────┐
│ SERVICE LAYER (Camada de Serviços)                                     │
├───────────────────────────┼─────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  AccessService (Application Layer)                              │ │
│  │    HasAccessAsync(userId, now)                                  │ │
│  │      └─ Chama IAccessRuleRepository.GetByUserIdAsync()          │ │
│  └──────────────────────┬───────────────────────────────────────────┘ │
│                         │                                             │
│  ┌──────────────────────┼───────────────────────────────────────────┐ │
│  │  JwtService (Security Layer)                                    │ │
│  │    GenerateToken(), ValidateToken()                             │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                         │                                             │
└─────────────────────────┼─────────────────────────────────────────────┘
                          │
┌─────────────────────────┼─────────────────────────────────────────────┐
│ DATA ACCESS LAYER (Camada de Acesso)                                 │
├─────────────────────────┼─────────────────────────────────────────────┤
│  ┌──────────────────────┼───────────────────────────────────────────┐ │
│  │  AccessRuleRepository (Infrastructure Layer)                    │ │
│  │    ├─ GetByUserIdAsync(userId)  ← ✨ COM LOGS                  │ │
│  │    ├─ GetAllAsync()             ← ✨ COM LOGS                  │ │
│  │    ├─ AddAsync()                ← ✨ COM LOGS                  │ │
│  │    ├─ UpdateAsync()             ← ✨ COM LOGS                  │ │
│  │    └─ DeleteAsync()             ← ✨ COM LOGS                  │ │
│  └──────────────────────┬───────────────────────────────────────────┘ │
│                         │                                             │
│  ┌──────────────────────┼───────────────────────────────────────────┐ │
│  │  Entity Framework Core (ORM)                                    │ │
│  │    ├─ Npgsql (Driver PostgreSQL)                                │ │
│  │    ├─ Connection String: localhost:5432                         │ │
│  │    └─ Retry Policy: 5 tentativas                                │ │
│  └──────────────────────┬───────────────────────────────────────────┘ │
│                         │                                             │
└─────────────────────────┼─────────────────────────────────────────────┘
                          │ SQL Query
                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ DATABASE LAYER (Camada de Banco de Dados)                             │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  PostgreSQL 16 (Docker Container)                              │  │
│  │  ├─ Banco: CameraAccessDb                                      │  │
│  │  └─ Tabela: AccessRules                                        │  │
│  │     ├─ id (UUID)                                               │  │
│  │     ├─ UserId (VARCHAR)                                        │  │
│  │     ├─ StreamName (VARCHAR)                                    │  │
│  │     ├─ Days (VARCHAR) → "Mon,Tue,Wed,..."                     │  │
│  │     ├─ Start (TIME) → "08:00:00"                              │  │
│  │     ├─ End (TIME) → "18:00:00"                                │  │
│  │     └─ Índices: UserId, StreamName                             │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  pgAdmin (Web Interface) - Porta 5050                           │  │
│  │  ├─ Visualizar dados                                           │  │
│  │  ├─ Executar queries SQL                                       │  │
│  │  ├─ Adicionar/editar/deletar registros                        │  │
│  │  ├─ Monitorar performance                                      │  │
│  │  └─ Fazer backup/restore                                       │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Fluxo de uma Requisição

```
1️⃣  CLIENT
    curl http://localhost:5001/watch/user1

2️⃣  MIDDLEWARE PIPELINE
    ├─ IpRateLimiting
    │  └─ Verifica: 10 req/min por IP ✓
    │
    ├─ ExceptionMiddleware (Tenta-catch)
    │  └─ Se erro, detecta tipo e oferece sugestões
    │
    └─ HTTPS Redirect (opcional)

3️⃣  CONTROLLER
    WatchController.GetAccess("user1")
    ├─ Recebe: userId = "user1"
    ├─ Log: "👤 Recebido GET /watch/user1"
    └─ Chama: AccessService.HasAccessAsync()

4️⃣  SERVICE
    AccessService.HasAccessAsync("user1", DateTime.Now)
    ├─ Obtém hora atual
    ├─ Chama: Repository.GetByUserIdAsync("user1")
    └─ Verifica: está no horário permitido?

5️⃣  REPOSITORY
    AccessRuleRepository.GetByUserIdAsync("user1")
    ├─ Log: "🔍 Consultando regra para user1"
    ├─ Executa SQL:
    │  SELECT * FROM "AccessRules" 
    │  WHERE "UserId" = 'user1'
    └─ Log: "✅ Regra encontrada"

6️⃣  DATABASE
    PostgreSQL.Query()
    ├─ Procura "user1" na tabela
    ├─ Encontra: 
    │  {
    │    UserId: "user1",
    │    StreamName: "stream_main",
    │    Days: "Mon,Tue,Wed,Thu,Fri",
    │    Start: "08:00:00",
    │    End: "18:00:00"
    │  }
    └─ Retorna resultado

7️⃣  SERVICE (Validação)
    Valida se (DateTime.Now está entre Start e End) 
    E se (dia atual está em Days)
    ├─ Se SIM: hasAccess = true
    └─ Se NÃO: hasAccess = false

8️⃣  RESPONSE
    HTTP 200 OK
    {
      "hasAccess": true,
      "message": "Acesso permitido",
      "userId": "user1"
    }

9️⃣  LOGGING
    Log: "✅ Acesso concedido para user1"
    ├─ Timestamp: 2026-05-04 13:45:23.456
    ├─ Level: Information
    ├─ CorrelationId: abc123def456
    └─ Arquivo: logs/log-2026-05-04.txt
```

---

## 📋 Estrutura de Dados

```
╔═══════════════════════════════════════════════════════════════╗
║                      Tabela: AccessRules                      ║
╠═══════════════════════════════════════════════════════════════╣
║ id (UUID)          | Primary Key, Auto-generated              ║
║ UserId (VARCHAR)   | Identificador do usuário                 ║
║ StreamName (VAR)   | Nome do stream RTSP                      ║
║ Days (VARCHAR)     | Dias permitidos "Mon,Tue,Wed,..."        ║
║ Start (TIME)       | Hora de início "08:00:00"                ║
║ End (TIME)         | Hora de término "18:00:00"               ║
║ created_at (TS)    | Quando foi criado                        ║
║ updated_at (TS)    | Quando foi atualizado                    ║
╠═══════════════════════════════════════════════════════════════╣
║ Índices:           | UserId, StreamName (para performance)    ║
╚═══════════════════════════════════════════════════════════════╝

Dados de Exemplo:
┌────────┬──────────────┬─────────────────┬──────────┬──────────┐
│ UserId │ StreamName   │ Days            │ Start    │ End      │
├────────┼──────────────┼─────────────────┼──────────┼──────────┤
│ user1  │ stream_main  │ Mon,Tue,Wed,... │ 08:00:00 │ 18:00:00 │
│ user2  │ stream_backup│ Sat,Sun         │ 10:00:00 │ 20:00:00 │
│ user3  │ stream_main  │ Mon,Wed,Fri     │ 06:00:00 │ 22:00:00 │
└────────┴──────────────┴─────────────────┴──────────┴──────────┘
```

---

## 🏢 Organização de Pastas

```
CameraAccessAPI/
│
├─📁 Application/             (Serviços e lógica de negócio)
│  └─ Services/
│     └─ AccessService.cs
│
├─📁 Controllers/             (Camada de apresentação)
│  └─ WatchController.cs
│
├─📁 Domain/                  (Entidades e interfaces)
│  ├─ Entities/
│  │  └─ AccessRule.cs
│  └─ Interfaces/
│     └─ IAccessRuleRepository.cs
│
├─📁 Infrastructure/          (Acesso a dados)
│  ├─ Persistence/
│  │  ├─ ApplicationDbContext.cs
│  │  └─ FakeDb.cs
│  └─ Repositories/
│     └─ AccessRuleRepository.cs
│
├─📁 Presentation/            (Middlewares e tratamento)
│  └─ Middleware/
│     └─ ExceptionMiddleware.cs
│
├─📁 Security/                (Autenticação e JWT)
│  └─ JwtService.cs
│
├─📁 logs/                    (Logs estruturados - Serilog)
│  └─ log-YYYY-MM-DD.txt
│
├─ Program.cs                 (Entry point principal)
├─ appsettings.json           (Config produção)
├─ appsettings.Development.json (Config desenvolvimento)
├─ docker-compose.yml         (Orquestração containers)
├─ init.sql                   (Script inicialização banco)
├─ setup-project.ps1          (Setup automático)
└─ debug-postgres.ps1         (Debug script)
```

---

## 🐳 Docker Compose Topology

```
┌──────────────────────────────────────────────────────────────┐
│                     Docker Network (Default Bridge)          │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────────┐     ┌──────────────────────┐     │
│  │  PostgreSQL          │     │  pgAdmin             │     │
│  ├──────────────────────┤     ├──────────────────────┤     │
│  │ Container:           │     │ Container:           │     │
│  │ camera_access_db     │     │ camera_pgadmin       │     │
│  │                      │     │                      │     │
│  │ Port: 5432           │     │ Port: 5050           │     │
│  │ User: postgres       │     │ Email:               │     │
│  │ Pass: yourpassword   │     │ admin@admin.com      │     │
│  │                      │◄────┤ Pass: admin123       │     │
│  │ DB: CameraAccessDb   │     │                      │     │
│  │ Tables: AccessRules  │     │ Depends: postgres ✓  │     │
│  │                      │     │                      │     │
│  │ Volume:              │     │ Volume:              │     │
│  │ postgres_data        │     │ pgadmin_data         │     │
│  └──────────────────────┘     └──────────────────────┘     │
│          ▲                                                   │
│          │ init.sql                                         │
│          │ (Cria tabelas)                                   │
│          │                                                   │
│  ┌───────┴──────────────────────┐                          │
│  │  MediaMTX                    │                          │
│  ├──────────────────────────────┤                          │
│  │ Container:                   │                          │
│  │ camera_mediamtx              │                          │
│  │                              │                          │
│  │ Port: 8554 (RTSP)            │                          │
│  │ Port: 8888 (HTTP)            │                          │
│  │ Port: 8889                   │                          │
│  │                              │                          │
│  │ Volume:                      │                          │
│  │ mediamtx.yml                 │                          │
│  └──────────────────────────────┘                          │
│                                                              │
└──────────────────────────────────────────────────────────────┘

LOCAL MACHINE
┌──────────────────────────────────────────────────────────────┐
│  localhost:5432  ◄── PostgreSQL                              │
│  localhost:5050  ◄── pgAdmin (http://localhost:5050)         │
│  localhost:5001  ◄── API Dotnet                              │
│  localhost:8554  ◄── MediaMTX RTSP                           │
│  localhost:8888  ◄── MediaMTX HTTP                           │
└──────────────────────────────────────────────────────────────┘
```

---

## 🔐 Segurança - Fluxo JWT (Future)

```
CLIENT
  ├─ POST /auth/login
  │  ├─ Credenciais: user/pass
  │  └─ Response: { token: "jwt_token", expiresIn: 300 }
  │
  └─ GET /watch/{userId}
     ├─ Header: Authorization: Bearer jwt_token
     ├─ JwtService: Valida token
     │  └─ Se válido: continua
     │  └─ Se expirado: 401 Unauthorized
     └─ Acessa recurso

Tokens:
  Expiração: 5 minutos
  Chave: SUPER_SECRET_KEY_12345678901234567890123456789012
  Issuer: CameraAccessAPI
  Audience: CameraClients
```

---

## 📊 Logging Estruturado (Serilog)

```
Estrutura do Log
┌────────────────────────────────────────────────────────────┐
│ [2026-05-04 13:45:23.456 +00:00] [INF] [Development]      │
│ ✅ Conexão com PostgreSQL estabelecida com sucesso          │
└────────────────────────────────────────────────────────────┘

Campos:
  Timestamp: quando ocorreu
  Level: Sev/INF/WRN/ERR/FTL
  Environment: Development/Production
  Message: descrição do evento
  Exception: stack trace se houver

Armazenamento:
  Console: Tempo real colorido
  Arquivo: logs/log-2026-05-04.txt
  Rotação: Diária
  Retenção: 7 dias

CorrelationId:
  Rastreia requisição do início ao fim
  Permite auditoria completa
  Facilita debugging
```

---

## ✅ Checklist de Componentes

- ✅ API (ASP.NET Core) - Implementada
- ✅ Controllers (REST Endpoints) - Implementado
- ✅ Services (Lógica) - Implementado
- ✅ Repositories (EF Core) - Implementado
- ✅ Database (PostgreSQL) - Container
- ✅ pgAdmin (Web UI) - Container
- ✅ Logging (Serilog) - Implementado
- ✅ Exception Handling - Implementado
- ✅ Rate Limiting - Implementado
- ✅ Retry Policy - Implementado
- ✅ Health Check - Implementado
- ✅ Documentation - Completa

---

**Arquitetura escalável, segura e bem documentada!** 🚀
