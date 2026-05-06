# 🔧 Guia Prático: Resolver Problemas via pgAdmin

## 📌 Cenários Comuns e Soluções

---

## ❌ Erro: "Database connection failed"

### Causa
PostgreSQL não está conectando

### Solução via pgAdmin

#### 1. Verificar se PostgreSQL está rodando
```powershell
# Terminal PowerShell
docker ps | grep camera_access_db
```

Se não aparecer nada:
```powershell
docker-compose up -d
Start-Sleep -Seconds 10
```

#### 2. No pgAdmin, adicionar servidor
1. Clique em **Servers** (lado esquerdo)
2. **New** → **Server**
3. Aba **Connection**:
   - Hostname: `localhost`
   - Port: `5432`
   - Username: `postgres`
   - Password: `yourpassword`
4. Clique **Save**

Se der erro "Connection refused":
- PostgreSQL pode estar iniciando ainda (aguarde 20 segundos)
- Verifique `docker-compose logs postgres`

---

## ❌ Erro: "Banco não existe"

### Sintoma
Erro: `3D000 - database "CameraAccessDb" does not exist`

### Solução via pgAdmin

#### Opção 1: Criar via Query Tool
1. No pgAdmin, clique em servidor PostgreSQL
2. **Tools** → **Query Tool**
3. Execute:
```sql
CREATE DATABASE "CameraAccessDb";
```
4. Pressione **Play** (F5)
5. Clique em **Refresh** para atualizar a lista de bancos

#### Opção 2: Remover e recriar containers
```powershell
docker-compose down -v   # Remove volumes (dados antigos)
docker-compose up -d
Start-Sleep -Seconds 15
```

---

## ❌ Erro: "Tabela não existe"

### Sintoma
```
relation "AccessRules" does not exist
```

### Solução via pgAdmin

#### 1. Verificar se tabela existe
1. Conecte ao banco **CameraAccessDb**
2. Vá em **Schemas** → **public** → **Tables**
3. Procure por **AccessRules**

Se não encontrar:

#### 2. Recriar tabelas
1. Tools → **Query Tool**
2. Execute:

```sql
-- Criar tabela AccessRules
CREATE TABLE IF NOT EXISTS "AccessRules" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" VARCHAR(100) NOT NULL,
    "StreamName" VARCHAR(200) NOT NULL,
    "Days" VARCHAR(50) NOT NULL,
    "Start" TIME NOT NULL,
    "End" TIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Criar índices
CREATE INDEX IF NOT EXISTS idx_access_rules_user_id ON "AccessRules"("UserId");
CREATE INDEX IF NOT EXISTS idx_access_rules_stream_name ON "AccessRules"("StreamName");

-- Inserir dados de exemplo
INSERT INTO "AccessRules" ("UserId", "StreamName", "Days", "Start", "End") VALUES
('user1', 'stream_main', 'Mon,Tue,Wed,Thu,Fri', '08:00:00', '18:00:00'),
('user2', 'stream_backup', 'Sat,Sun', '10:00:00', '20:00:00'),
('user3', 'stream_main', 'Mon,Wed,Fri', '06:00:00', '22:00:00');
```

3. Pressione **F5** para executar

---

## ❌ Erro: "Dados corrompidos ou desincronizados"

### Solução via pgAdmin

#### 1. Limpar tudo e recriar
```sql
-- ATENÇÃO: Isto deletará TODOS os dados!
DROP TABLE IF EXISTS "AccessRules" CASCADE;

-- Depois executar o SQL de recriação acima
```

#### 2. Ou apenas deletar dados de exemplo
```sql
DELETE FROM "AccessRules" WHERE "UserId" IN ('user1', 'user2', 'user3');
```

#### 3. Inserir novos dados
```sql
INSERT INTO "AccessRules" ("UserId", "StreamName", "Days", "Start", "End") VALUES
('user1', 'stream_main', 'Mon,Tue,Wed,Thu,Fri', '08:00:00', '18:00:00');
```

---

## ✅ Verificar Saúde do Banco

Execute no pgAdmin → Query Tool:

```sql
-- 1. Listar todas as tabelas
SELECT * FROM information_schema.tables WHERE table_schema = 'public';

-- 2. Contar registros em AccessRules
SELECT COUNT(*) as total_records FROM "AccessRules";

-- 3. Ver estrutura da tabela
\d "AccessRules"
-- Ou via SQL:
SELECT column_name, data_type FROM information_schema.columns 
WHERE table_name = 'AccessRules';

-- 4. Listar todos os usuários
SELECT DISTINCT "UserId" FROM "AccessRules";

-- 5. Verificar índices
SELECT indexname FROM pg_indexes WHERE tablename = 'AccessRules';

-- 6. Verificar espaço de disco usado
SELECT 
    pg_size_pretty(pg_database_size('CameraAccessDb')) AS database_size,
    pg_size_pretty(pg_total_relation_size('AccessRules')) AS table_size;
```

---

## 🔐 Segurança: Alterar Senhas

### Alterar senha PostgreSQL

```sql
-- No pgAdmin Query Tool, conectado como postgres:
ALTER USER postgres WITH PASSWORD 'nova_senha_segura';
```

Depois atualize:
1. `docker-compose.yml`: `POSTGRES_PASSWORD: nova_senha_segura`
2. `appsettings.Development.json`: `Password=nova_senha_segura`
3. Reinicie: `docker-compose restart postgres`

### Alterar senha pgAdmin

1. Você precisa do email de admin

Edite `docker-compose.yml`:
```yaml
pgadmin:
  environment:
    PGADMIN_DEFAULT_PASSWORD: nova_senha_super_segura
```

Reinicie:
```powershell
docker-compose restart pgadmin
```

---

## 📊 Monitorar Performance

### Ver logs do servidor
1. Clique em servidor PostgreSQL em pgAdmin
2. **Tools** → **Server Log**

### Ver conexões ativas
```sql
SELECT 
    pid, 
    usename, 
    application_name, 
    state, 
    query 
FROM pg_stat_activity 
WHERE datname = 'CameraAccessDb';
```

### Ver queries lentas
```sql
-- Ativar slow query log
ALTER SYSTEM SET log_min_duration_statement = 1000; -- 1 segundo
SELECT pg_reload_conf();

-- Depois em Query Tool, ver logs:
SELECT * FROM pg_stat_statements ORDER BY total_time DESC LIMIT 10;
```

---

## 🔄 Backup e Restore

### Via pgAdmin

#### Backup
1. Clique com botão direito em **CameraAccessDb**
2. **Backup**
3. Escolha formato "Custom"
4. Salve o arquivo .backup

#### Restore
1. Clique com botão direito em **CameraAccessDb**
2. **Restore**
3. Selecione arquivo .backup

### Via Terminal
```powershell
# Backup
docker exec camera_access_db pg_dump -U postgres -d CameraAccessDb > backup.sql

# Restore
docker exec -i camera_access_db psql -U postgres -d CameraAccessDb < backup.sql
```

---

## 🎯 Checklist: Projeto Rodando Corretamente

Execute estas queries no pgAdmin para confirmar tudo:

```sql
-- 1. ✅ Banco existe
SELECT 1 FROM pg_database WHERE datname = 'CameraAccessDb';

-- 2. ✅ Tabela existe
SELECT 1 FROM information_schema.tables WHERE table_name = 'AccessRules';

-- 3. ✅ Dados existem
SELECT COUNT(*) FROM "AccessRules";

-- 4. ✅ Índices existem
SELECT COUNT(*) FROM pg_indexes WHERE tablename = 'AccessRules';

-- 5. ✅ Usuários de exemplo existem
SELECT DISTINCT "UserId" FROM "AccessRules" ORDER BY "UserId";

-- 6. ✅ Conectar e testar permissões
SELECT current_user, current_database();
```

Se todos retornarem dados, tudo está funcionando! ✅

---

## 🚑 Emergência: Resetar Tudo

```powershell
# 1. Parar containers
docker-compose down -v

# 2. Aguardar
Start-Sleep -Seconds 5

# 3. Reiniciar
docker-compose up -d

# 4. Esperar ser criado
Start-Sleep -Seconds 20

# 5. Verificar
docker ps | grep camera

# 6. Abrir pgAdmin
# http://localhost:5050
```

---

## 📞 Suporte

| Problema | Comando para Debug |
|----------|-------------------|
| PostgreSQL não responde | `docker logs camera_access_db` |
| pgAdmin não abre | `docker logs camera_pgadmin` |
| Verificar containers | `docker ps` |
| Ver erros recentes | `docker-compose logs` |
| Reiniciar tudo | `docker-compose down && docker-compose up -d` |

---

**Duvida? Abra pgAdmin e explore! É muito intuitivo.** 🚀
