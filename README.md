# Camera Access API

Sistema de controle de acesso a streams de vídeo baseado em regras de horário.

## 🚀 Quick Start

### Opção 1: Setup Automatizado (Recomendado)
```powershell
.\setup-project.ps1
```

### Opção 2: Manual
```powershell
# 1. Iniciar Docker com PostgreSQL + pgAdmin
docker-compose up -d

# 2. Abrir pgAdmin (http://localhost:5050)
# Email: admin@admin.com | Senha: admin123

# 3. Executar API
dotnet run

# 4. Testar
curl http://localhost:5001/watch/user1
```

---

## 📊 Arquitetura

- **Domain**: Entidades, interfaces e lógica de negócio
- **Application**: Serviços de aplicação e casos de uso
- **Infrastructure**: Persistência, repositórios e integrações
- **Presentation**: Controllers, middlewares e APIs
- **Security**: JWT e autenticação

---

## 🛠️ Tecnologias

| Tecnologia | Versão | Propósito |
|-----------|--------|----------|
| .NET | 10.0 | Framework principal |
| ASP.NET Core | 10.0 | Web API |
| Entity Framework Core | 10.0 | ORM |
| PostgreSQL | 16 | Banco de dados |
| Npgsql | - | Driver PostgreSQL |
| Serilog | - | Logging estruturado |
| JWT | - | Autenticação |
| AspNetCoreRateLimit | - | Rate limiting |
| MediaMTX | Latest | Streaming de vídeo |

---

## 🗄️ Banco de Dados

### Configuração Automática
1. ✅ PostgreSQL criado em container
2. ✅ Banco `CameraAccessDb` criado automaticamente
3. ✅ Tabela `AccessRules` criada com dados de exemplo
4. ✅ Índices criados para performance

### Credenciais PostgreSQL
```
Host:     localhost
Port:     5432
User:     postgres
Password: yourpassword
Database: CameraAccessDb
```

### Gerenciar via pgAdmin
```
URL:  http://localhost:5050
User: admin@admin.com
Pass: admin123
```

**Guia completo:** [PGADMIN_GUIDE.md](PGADMIN_GUIDE.md)

---

## 📝 Configuração

### Arquivos de Configuração

#### appsettings.json
- Connection string de produção
- Senhas são **placeholders**
- Nunca fazer commit com senhas reais

#### appsettings.Development.json
- Connection string de desenvolvimento
- Senha sincronizada: `yourpassword`
- Logs em nível Debug
- **Nunca fazer commit com este arquivo**

### Docker Compose
```yaml
services:
  - postgres: PostgreSQL 16
  - pgadmin: Interface web
  - mediamtx: Streaming RTSP
```

---

## 🔌 Endpoints

| Método | Endpoint | Descrição | Query Params |
|--------|----------|-----------|--------------|
| GET | `/watch/{userId}` | Verificar acesso ao stream | - |
| GET | `/swagger` | Documentação OpenAPI | - |

### Exemplo
```bash
curl http://localhost:5001/watch/user1

Resposta:
{
  "hasAccess": true,
  "message": "Acesso permitido",
  "userId": "user1"
}
```

---

## 🔐 Segurança

### Autenticação JWT
- Tokens com expiração de 5 minutos
- Chave secreta em `appsettings.json`
- Validação em cada requisição

### Rate Limiting
- Limite: 10 requisições por minuto por IP
- Retorna status 429 se excedido
- Configurável em `appsettings.json`

### Middleware de Exceção
- Todos os erros registrados com CorrelationId
- Logs estruturados com Serilog
- Respostas com sugestões de debug

---

## 📋 Estrutura de Dados

### Tabela AccessRules

```sql
CREATE TABLE "AccessRules" (
    id UUID PRIMARY KEY,
    "UserId" VARCHAR(100) NOT NULL,
    "StreamName" VARCHAR(200) NOT NULL,
    "Days" VARCHAR(50) NOT NULL,
    "Start" TIME NOT NULL,
    "End" TIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Dados de Exemplo
```
user1  | stream_main   | Mon,Tue,Wed,Thu,Fri | 08:00:00 | 18:00:00
user2  | stream_backup | Sat,Sun             | 10:00:00 | 20:00:00
user3  | stream_main   | Mon,Wed,Fri         | 06:00:00 | 22:00:00
```

---

## 📊 Logs

### Estrutura de Log
```
[2026-05-04 13:17:49.123 +00:00] [INF] [Development] Conexão com PostgreSQL estabelecida
```

### Níveis em Development
- **Debug**: Queries SQL detalhadas
- **Information**: Eventos importantes
- **Warning**: Situações inesperadas
- **Error**: Erros que precisam atenção
- **Fatal**: Erros críticos que matam a app

### Arquivo de Log
```
logs/log-2026-05-04.txt   (rotativo diário)
```

---

## 🧪 Testes

Executar testes:
```powershell
dotnet test
```

---

## 🐛 Troubleshooting

### Erro: "Connection refused"
```powershell
docker ps | grep camera_access_db
docker-compose up -d
```

### Erro: "Password authentication failed"
1. Verifique: `appsettings.Development.json`
2. Verifique: `docker-compose.yml`
3. Ambos devem ter: `password: yourpassword`

### Erro: "Database does not exist"
Abra pgAdmin (http://localhost:5050) e execute:
```sql
CREATE DATABASE "CameraAccessDb";
```

### Mais ajuda
- [QUICKSTART.md](QUICKSTART.md) - Início rápido
- [PGADMIN_GUIDE.md](PGADMIN_GUIDE.md) - Guia pgAdmin
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Soluções detalhadas
- [SOLUTION.md](SOLUTION.md) - Análise técnica

---

## 🚀 Fluxo de Desenvolvimento

```
┌─────────────────────────────────────────┐
│  1. Iniciar Docker                      │
│     docker-compose up -d                │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  2. Explorar dados no pgAdmin           │
│     http://localhost:5050               │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  3. Executar API                        │
│     dotnet run                          │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  4. Testar endpoints                    │
│     curl http://localhost:5001/watch/.. │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  5. Monitorar logs                      │
│     Console + logs/log-YYYY-MM-DD.txt  │
└─────────────────────────────────────────┘
```

---

## 📂 Estrutura de Pastas

```
CameraAccessAPI/
├── Application/
│   └── Services/
│       └── AccessService.cs
├── Controllers/
│   └── WatchController.cs
├── Domain/
│   ├── Entities/
│   │   └── AccessRule.cs
│   └── Interfaces/
│       └── IAccessRuleRepository.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   └── FakeDb.cs
│   └── Repositories/
│       └── AccessRuleRepository.cs
├── Presentation/
│   └── Middleware/
│       └── ExceptionMiddleware.cs
├── Security/
│   └── JwtService.cs
├── docker-compose.yml
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

---

## 🎯 Próximos Passos

1. ✅ Ambiente pronto com `setup-project.ps1`
2. 📊 Explorar dados no pgAdmin
3. 🧪 Testar endpoints via curl/Postman
4. 📝 Adicionar novos usuários ao banco
5. 🔐 Implementar autenticação JWT completa
6. 🎬 Integrar MediaMTX para streaming

---

## 📞 Referências

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [PostgreSQL Documentation](https://www.postgresql.org/docs)
- [pgAdmin Documentation](https://www.pgadmin.org/docs)
- [Serilog](https://serilog.net)

---

## 🤝 Suporte

Se encontrar problemas:
1. Verifique [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
2. Verifique logs: `logs/log-*.txt`
3. Use pgAdmin para inspecionar banco
4. Verifique Docker: `docker ps`

---

**Pronto! Seu ambiente está totalmente configurado! 🚀**