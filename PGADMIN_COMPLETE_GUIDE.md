# 📋 CameraAccessAPI - Guia Completo pgAdmin

## 🎯 **OBJETIVO**
Este guia mostra exatamente como criar todas as tabelas necessárias no pgAdmin para o projeto CameraAccessAPI funcionar completamente.

---

## 📋 **TABELAS A CRIAR (7 tabelas principais)**

| Tabela | Propósito | Campos Chave |
|--------|-----------|--------------|
| **Users** | Usuários do sistema | id, username, email |
| **Streams** | Câmeras/Streams | id, name, url |
| **AccessRules** | Regras de acesso | user_id, stream_id, days_of_week |
| **AuditLogs** | Logs de auditoria | action, user_id, timestamp |
| **FailedAttempts** | Tentativas falhidas | username, ip, count |
| **UserBlocks** | Bloqueios de usuário | user_id, reason, expires_at |
| **ActiveSessions** | Sessões ativas | user_id, token_hash, expires_at |

---

## 🚀 **PASSO A PASSO NO PGADMIN**

### **1. Conectar ao PostgreSQL**
```
Host: localhost
Port: 5432
Database: CameraAccessDb
Username: postgres
Password: yourpassword
```

### **2. Executar Script Completo**

#### **Opção A: Script Automático (Recomendado)**
```sql
-- Copie e cole TODO o conteúdo do arquivo complete_schema_v3.sql
-- Execute tudo de uma vez
\i /caminho/para/complete_schema_v3.sql
```

#### **Opção B: Passo a Passo Manual**

**2.1 Criar Extensões:**
```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
```

**2.2 Criar Tabela Users:**
```sql
CREATE TABLE "Users" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_username_length CHECK (char_length(username) >= 3),
    CONSTRAINT chk_email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);
```

**2.3 Criar Tabela Streams:**
```sql
CREATE TABLE "Streams" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) UNIQUE NOT NULL,
    description TEXT,
    url VARCHAR(500),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_stream_name_length CHECK (char_length(name) >= 1)
);
```

**2.4 Criar Tabela AccessRules:**
```sql
CREATE TABLE "AccessRules" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    stream_id UUID NOT NULL REFERENCES "Streams"(id) ON DELETE CASCADE,
    days_of_week INTEGER[] NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_days_range CHECK (array_length(days_of_week, 1) > 0 AND array_length(days_of_week, 1) <= 7),
    CONSTRAINT chk_time_range CHECK (start_time < end_time),
    CONSTRAINT chk_days_values CHECK (days_of_week <@ ARRAY[0,1,2,3,4,5,6])
);
```

**2.5 Criar Tabelas de Segurança:**
```sql
-- AuditLogs
CREATE TABLE "AuditLogs" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id) ON DELETE SET NULL,
    action VARCHAR(50) NOT NULL,
    resource VARCHAR(100),
    resource_id UUID,
    ip_address INET,
    user_agent TEXT,
    details JSONB,
    success BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- FailedAttempts
CREATE TABLE "FailedAttempts" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL,
    ip_address INET NOT NULL,
    user_agent TEXT,
    reason VARCHAR(100),
    attempt_count INTEGER DEFAULT 1,
    first_attempt_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_attempt_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_blocked BOOLEAN DEFAULT false,
    blocked_until TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- UserBlocks
CREATE TABLE "UserBlocks" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id) ON DELETE CASCADE,
    reason VARCHAR(100) NOT NULL,
    blocked_by UUID REFERENCES "Users"(id),
    blocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ActiveSessions
CREATE TABLE "ActiveSessions" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    token_hash VARCHAR(128) UNIQUE NOT NULL,
    ip_address INET,
    user_agent TEXT,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_token_hash_length CHECK (char_length(token_hash) = 64)
);
```

### **3. Criar Índices (Performance)**

```sql
-- Índices essenciais para performance
CREATE INDEX idx_access_rules_user_id ON "AccessRules"(user_id);
CREATE INDEX idx_access_rules_stream_id ON "AccessRules"(stream_id);
CREATE INDEX idx_access_rules_days ON "AccessRules" USING GIN(days_of_week);
CREATE INDEX idx_audit_logs_created_at ON "AuditLogs"(created_at);
CREATE INDEX idx_failed_attempts_username ON "FailedAttempts"(username);
CREATE INDEX idx_active_sessions_expires_at ON "ActiveSessions"(expires_at);
```

### **4. Inserir Dados de Exemplo**

```sql
-- Usuários
INSERT INTO "Users" (username, email) VALUES
('admin', 'admin@cameraaccess.com'),
('user1', 'user1@empresa.com'),
('user2', 'user2@empresa.com');

-- Streams
INSERT INTO "Streams" (name, description) VALUES
('stream_main', 'Câmera principal'),
('stream_backup', 'Câmera backup');

-- Regras de acesso (exemplo)
INSERT INTO "AccessRules" (user_id, stream_id, days_of_week, start_time, end_time)
SELECT u.id, s.id, ARRAY[1,2,3,4,5], '08:00:00'::TIME, '18:00:00'::TIME
FROM "Users" u, "Streams" s
WHERE u.username = 'user1' AND s.name = 'stream_main';
```

---

## 🔍 **VERIFICAÇÃO NO PGADMIN**

### **Verificar Tabelas Criadas:**
```sql
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
ORDER BY table_name;
```

**Resultado esperado:**
```
AccessRules
ActiveSessions
AuditLogs
FailedAttempts
Streams
UserBlocks
Users
```

### **Verificar Dados Inseridos:**
```sql
-- Ver usuários
SELECT id, username, email FROM "Users";

-- Ver regras de acesso
SELECT u.username, s.name, ar.days_of_week, ar.start_time, ar.end_time
FROM "AccessRules" ar
JOIN "Users" u ON ar.user_id = u.id
JOIN "Streams" s ON ar.stream_id = s.id;
```

---

## ⚡ **TESTE DA APLICAÇÃO**

### **1. Testar Conexão:**
```bash
# No terminal do projeto
dotnet run
```

### **2. Testar API:**
```bash
curl http://localhost:5001/watch/user1
```

**Resposta esperada:**
```json
{
  "stream": "http://localhost:8888/user1?token=...",
  "expiresInSeconds": 300
}
```

---

## 🔒 **SEGURANÇA IMPLEMENTADA**

| Recurso | Descrição |
|---------|-----------|
| **Constraints** | Validações automáticas de dados |
| **Índices** | Consultas otimizadas |
| **Auditoria** | Logs de todas as operações |
| **Rate Limiting** | Controle de tentativas falhidas |
| **Bloqueios** | Sistema de bloqueio de usuários |
| **Sessões** | Controle de sessões ativas |

---

## 🚨 **PROBLEMAS COMUNS E SOLUÇÕES**

### **Erro: "relation does not exist"**
```sql
-- Verificar se tabelas existem
\dt
```

### **Erro: "permission denied"**
```sql
-- Conectar como postgres
\c CameraAccessDb postgres
```

### **Erro: "duplicate key value"**
```sql
-- Limpar dados existentes
TRUNCATE "AccessRules", "Users", "Streams" CASCADE;
```

---

## 📊 **MONITORAMENTO**

### **Ver Estatísticas:**
```sql
-- Ver estrutura completa
SELECT schemaname, tablename, attname, typname
FROM pg_attribute a
JOIN pg_class c ON a.attrelid = c.oid
JOIN pg_type t ON a.atttypid = t.oid
JOIN pg_namespace n ON c.relnamespace = n.oid
WHERE n.nspname = 'public'
  AND c.relname IN ('Users', 'AccessRules', 'Streams')
  AND a.attnum > 0
ORDER BY c.relname, a.attnum;
```

### **Ver Logs de Auditoria:**
```sql
SELECT action, resource, created_at, details
FROM "AuditLogs"
ORDER BY created_at DESC
LIMIT 10;
```

---

## ✅ **CHECKLIST FINAL**

- [ ] PostgreSQL rodando (porta 5432)
- [ ] pgAdmin conectado
- [ ] Database "CameraAccessDb" criada
- [ ] Script `complete_schema_v3.sql` executado
- [ ] Todas as 7 tabelas criadas
- [ ] Dados de exemplo inseridos
- [ ] Índices criados
- [ ] API testada com sucesso
- [ ] Logs de auditoria funcionando

---

**🎉 PRONTO!** Seu banco de dados está completamente configurado e seguro para o CameraAccessAPI.