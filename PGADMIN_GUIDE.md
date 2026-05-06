# 🗄️ Guia Completo: pgAdmin para CameraAccessAPI

## 📌 O que é pgAdmin?

pgAdmin é uma ferramenta web para gerenciar bancos de dados PostgreSQL. Permite:
- ✅ Visualizar estrutura do banco
- ✅ Executar queries SQL
- ✅ Gerenciar usuários e permissões
- ✅ Backup/Restore
- ✅ Monitorar performance

---

## 🚀 Quick Start (3 Passos)

### 1️⃣ Inicie o Docker
```powershell
docker-compose up -d
```

### 2️⃣ Abra pgAdmin no browser
```
http://localhost:5050
```

### 3️⃣ Faça login
```
Email:    admin@admin.com
Senha:    admin123
```

---

## 🔧 Configurar Conexão com PostgreSQL

### Após fazer login no pgAdmin:

#### **Passo 1: Adicionar Servidor**
1. Clique em **Servers** → **New** → **Server**

#### **Passo 2: Aba "General"**
- **Name:** `CameraAccessDB` (ou qualquer nome)
- Clique em **Next** ou vá para aba **Connection**

#### **Passo 3: Aba "Connection"**
```
Hostname/address:    localhost    (ou 127.0.0.1)
Port:                5432
Maintenance database: postgres
Username:            postgres
Password:            yourpassword
```

#### **Passo 4: Salvar**
- Clique em **Save**
- Se der erro de conexão, volte ao Passo 3 e verifique dados

---

## 📊 Verificar Dados Criados

Após conectar ao servidor:

### 1. Expandir servidor
```
CameraAccessDB → Databases → CameraAccessDb
```

### 2. Visualizar tabela
```
CameraAccessDb → Schemas → public → Tables → AccessRules
```

### 3. Ver dados
- Clique com botão direito em **AccessRules** → **View/Edit Data** → **All Rows**

Deve aparecer:
```
| user1 | stream_main  | Mon,Tue,Wed,Thu,Fri | 08:00:00 | 18:00:00 |
| user2 | stream_backup | Sat,Sun             | 10:00:00 | 20:00:00 |
| user3 | stream_main  | Mon,Wed,Fri         | 06:00:00 | 22:00:00 |
```

---

## 💻 Executar Queries SQL

### 1. Abrir Query Tool
- Clique em banco → **Tools** → **Query Tool**
- Ou clique com botão direito em tabela → **Query Tool**

### 2. Exemplos de Queries

#### Listar todos os usuários
```sql
SELECT * FROM "AccessRules";
```

#### Buscar acesso de um usuário
```sql
SELECT * FROM "AccessRules" WHERE "UserId" = 'user1';
```

#### Contar registros
```sql
SELECT COUNT(*) FROM "AccessRules";
```

#### Adicionar novo usuário
```sql
INSERT INTO "AccessRules" ("UserId", "StreamName", "Days", "Start", "End") 
VALUES ('user4', 'stream_new', 'Tue,Thu', '09:00:00', '17:00:00');
```

#### Atualizar dados
```sql
UPDATE "AccessRules" 
SET "Days" = 'Mon,Tue,Wed,Thu,Fri,Sat,Sun' 
WHERE "UserId" = 'user1';
```

#### Deletar registro
```sql
DELETE FROM "AccessRules" WHERE "UserId" = 'user4';
```

---

## 🐛 Troubleshooting

### Erro: "Connection refused"
```powershell
# Verificar se container está rodando
docker ps | grep camera_access_db

# Se não existir, inicie:
docker-compose up -d

# Esperar 10-15 segundos e tentar novamente
```

### Erro: "Password authentication failed"
- Verifique a senha em pgAdmin: `yourpassword`
- Verifique em `docker-compose.yml`: `POSTGRES_PASSWORD: yourpassword`

### Não vejo a tabela AccessRules
```sql
-- Execute no Query Tool:
SELECT * FROM information_schema.tables WHERE table_schema = 'public';

-- Se vazio, execute o init.sql manualmente
```

---

## 🎯 Workflow Completo do Projeto

```
┌─────────────────────────────────────────────────────────────┐
│                    CameraAccessAPI Flow                     │
└─────────────────────────────────────────────────────────────┘

1. Cliente → GET /watch/user1
   ↓
2. WatchController chama AccessService
   ↓
3. AccessService chama AccessRuleRepository
   ↓
4. Repositório consulta PostgreSQL (via EF Core)
   ↓
5. pgAdmin permite visualizar dados da consulta
   ↓
6. Response retorna ao cliente

```

---

## 📈 Operações Comuns no pgAdmin

| Operação | Como Fazer |
|----------|-----------|
| **Ver todos dados** | Clique direito em tabela → View/Edit Data → All Rows |
| **Executar SQL** | Tools → Query Tool |
| **Adicionar dados** | View/Edit Data → adicionar linhas |
| **Deletar dados** | View/Edit Data → marcar linhas → Delete |
| **Backup** | Clique direito em DB → Backup |
| **Restaurar** | Clique direito em DB → Restore |
| **Ver logs** | Tools → Server Log |

---

## 🔐 Segurança

### Mudar senha pgAdmin (Opcional)
```powershell
# No docker-compose.yml, altere:
PGADMIN_DEFAULT_PASSWORD: admin123
# Para algo mais seguro:
PGADMIN_DEFAULT_PASSWORD: sua_senha_forte

# Depois reinicie:
docker-compose restart pgadmin
```

### Mudar senha PostgreSQL (Opcional)
```powershell
# No docker-compose.yml e appsettings.Development.json

# 1. docker-compose.yml:
POSTGRES_PASSWORD: nova_senha

# 2. appsettings.Development.json:
"DefaultConnection": "...Password=nova_senha..."

# 3. Reinicie tudo:
docker-compose down
docker-compose up -d
```

---

## 📞 Contato com Serviços

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **API** | http://localhost:5001 | Sem autenticação |
| **pgAdmin** | http://localhost:5050 | admin@admin.com / admin123 |
| **PostgreSQL** | localhost:5432 | postgres / yourpassword |
| **MediaMTX** | http://localhost:8888 | Sem autenticação |

---

## ✅ Checklist de Setup

- [ ] Docker está instalado e rodando
- [ ] `docker-compose up -d` executado
- [ ] PostgreSQL está saudável: `docker ps | grep camera_access_db`
- [ ] pgAdmin acessível: http://localhost:5050
- [ ] Login pgAdmin funciona (admin@admin.com / admin123)
- [ ] Servidor PostgreSQL conectado em pgAdmin
- [ ] Banco CameraAccessDb visível
- [ ] Tabela AccessRules visível
- [ ] Dados de exemplo presentes (3 linhas)
- [ ] API rodando: `dotnet run`
- [ ] API respondendo: `curl http://localhost:5001/watch/user1`

---

## 🎓 Próximos Passos

1. **Explorar dados** via pgAdmin
2. **Fazer requisições** à API
3. **Monitorar logs** via pgAdmin (Tools → Server Log)
4. **Adicionar novos usuários** via pgAdmin ou API
5. **Implementar backup** de segurança

---

**Pronto! Você agora tem um ambiente completo de desenvolvimento com PostgreSQL + pgAdmin + API!** 🚀
