# 🎯 Guia Passo a Passo: Usar pgAdmin para o Projeto

## 📍 Índice
1. [Acessar pgAdmin](#1-acessar-pgadmin)
2. [Conectar ao PostgreSQL](#2-conectar-ao-postgresql)
3. [Visualizar Dados](#3-visualizar-dados)
4. [Adicionar/Editar Dados](#4-adicionareditar-dados)
5. [Executar Queries](#5-executar-queries)
6. [Troubleshooting](#6-troubleshooting)

---

## 1️⃣ Acessar pgAdmin

### Passo 1: Abra seu navegador

Vá para: **http://localhost:5050**

Você verá a tela de login do pgAdmin.

### Passo 2: Faça login

```
Email:  admin@admin.com
Senha:  admin123
```

Clique em **Login**

### Passo 3: Dashboard do pgAdmin

Após login, você verá a tela inicial com:
- **Browser** (lado esquerdo)
- **Dashboard** (centro)
- **Tools/Querys** (acima)

---

## 2️⃣ Conectar ao PostgreSQL

### Passo 1: Adicionar Novo Servidor

Na coluna esquerda:
1. Clique em **Servers**
2. Clique com botão direito
3. Selecione **New** → **Server**

Alternativa: Menu de três pontos next to "Servers"

### Passo 2: Preencher Informações

#### Aba "General"
```
Name:       CameraAccessDB
            (qualquer nome descritivo)
```

#### Aba "Connection"
```
Host name/address:  localhost
Port:               5432
Maintenance DB:     postgres
Username:           postgres
Password:           yourpassword
Save password?:     ✅ Sim (marque)
```

### Passo 3: Salvar

Clique em **Save**

Se conectou com sucesso:
- ✅ Servidor aparece em **Servers**
- ✅ Pode expandir para ver bancos

Se der erro:
- Verifique se Docker está rodando: `docker ps`
- Espere mais alguns segundos
- Verifique a senha

---

## 3️⃣ Visualizar Dados

### Passo 1: Expandir Estrutura

No Browser (esquerda):
```
Servers 
  └─ CameraAccessDB (seu servidor)
     └─ Databases
        └─ CameraAccessDb (clique para expandir)
           └─ Schemas
              └─ public (clique para expandir)
                 └─ Tables
                    └─ AccessRules (achamos!)
```

### Passo 2: Ver Dados

Clique com botão direito em **AccessRules**:
```
View/Edit Data
  └─ All Rows
```

Você verá uma tabela com:
```
| user1 | stream_main  | Mon,Tue,Wed,Thu,Fri | 08:00:00 | 18:00:00 |
| user2 | stream_backup | Sat,Sun             | 10:00:00 | 20:00:00 |
| user3 | stream_main  | Mon,Wed,Fri         | 06:00:00 | 22:00:00 |
```

### Passo 3: Voltar

Clique em **Servers** no Browser para voltar à hierarquia

---

## 4️⃣ Adicionar/Editar Dados

### Adicionar Novo Usuário

#### Via pgAdmin (Visual)
1. Clique com botão direito em **AccessRules**
2. **View/Edit Data** → **All Rows**
3. No final da tabela, veja uma linha em branco com `⊕`
4. Clique no `⊕` para adicionar nova linha
5. Preencha:
   ```
   UserId:    user4
   StreamName: stream_test
   Days:      Mon,Tue,Wed
   Start:     09:00:00
   End:       17:00:00
   ```
6. Clique fora ou pressione Enter para salvar

#### Via SQL (Query Tool)
Veja seção 5️⃣ abaixo

### Editar Usuário Existente

1. Em **View/Edit Data**, clique na célula que quer editar
2. Digite o novo valor
3. Pressione Enter ou clique fora

### Deletar Usuário

1. Em **View/Edit Data**, clique no número da linha (à esquerda)
2. Clique no ícone **🗑️** (delete) na toolbar
3. Confirme

---

## 5️⃣ Executar Queries

### Abrir Query Tool

#### Opção 1: Via Banco
1. Clique em **CameraAccessDb** (o banco)
2. Menu **Tools**
3. Selecione **Query Tool**

#### Opção 2: Via Tabela
1. Clique com botão direito em **AccessRules**
2. **Query Tool**

### Escrever Query

Na área de texto branca, você escreve SQL:

```sql
SELECT * FROM "AccessRules";
```

### Executar

- Pressione **F5** ou clique em ▶️ (Play)

### Ver Resultado

Aparece abaixo em "Data Output"

---

## 🔍 Exemplos Práticos de Queries

### 1. Listar Todos os Usuários
```sql
SELECT * FROM "AccessRules" ORDER BY "UserId";
```

### 2. Buscar Um Usuário Específico
```sql
SELECT * FROM "AccessRules" WHERE "UserId" = 'user1';
```

### 3. Listar Usuários com Acesso à Streams Específica
```sql
SELECT * FROM "AccessRules" WHERE "StreamName" = 'stream_main';
```

### 4. Contar Total de Registros
```sql
SELECT COUNT(*) as total_users FROM "AccessRules";
```

### 5. Listar Usuários que Têm Acesso NO Sábado
```sql
SELECT * FROM "AccessRules" WHERE "Days" LIKE '%Sat%';
```

### 6. Adicionar Novo Usuário
```sql
INSERT INTO "AccessRules" 
("UserId", "StreamName", "Days", "Start", "End") 
VALUES ('user5', 'stream_new', 'Mon,Tue,Wed,Thu,Fri', '07:00:00', '19:00:00');
```

### 7. Atualizar Horário de Usuário
```sql
UPDATE "AccessRules" 
SET "Start" = '09:00:00', "End" = '17:00:00' 
WHERE "UserId" = 'user1';
```

### 8. Deletar Usuário
```sql
DELETE FROM "AccessRules" WHERE "UserId" = 'user4';
```

### 9. Ver Estrutura da Tabela
```sql
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'AccessRules';
```

### 10. Exportar Dados como CSV
Resultado → Clique em "Download as CSV" (se disponível)

---

## 6️⃣ Troubleshooting

### ❌ Problema: "Could not connect to server"

**Solução:**

1. Verifique Docker:
```powershell
docker ps | grep camera_access_db
```

2. Se não aparecer:
```powershell
docker-compose up -d
Start-Sleep -Seconds 15
```

3. Tente novamente em pgAdmin

### ❌ Problema: Não vejo a tabela AccessRules

**Solução:**

1. Clique em **Refresh** (F5 no teclado)
2. Ou clique com botão direito em **Tables** → **Refresh**

Se ainda não aparecer:

1. Abra **Query Tool**
2. Execute:
```sql
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public';
```

3. Se não listar nada, a tabela não foi criada. Crie com:
```sql
CREATE TABLE "AccessRules" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" VARCHAR(100) NOT NULL,
    "StreamName" VARCHAR(200) NOT NULL,
    "Days" VARCHAR(50) NOT NULL,
    "Start" TIME NOT NULL,
    "End" TIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### ❌ Problema: "ERROR: relation 'AccessRules' does not exist"

**Solução:**

A tabela foi deletada. Recrie com SQL acima ou reinicie tudo:

```powershell
docker-compose down -v
docker-compose up -d
Start-Sleep -Seconds 20
```

### ❌ Problema: "ERROR: syntax error"

**Solução:**

Verifique:
- ✅ Nomes de coluna entre **aspas** (ex: `"UserId"`)
- ✅ String values entre **aspas simples** (ex: `'user1'`)
- ✅ Sem vírgula após último campo
- ✅ Ponto e vírgula `;` no final

### ❌ Problema: "permission denied"

**Solução:**

Seu usuário PostgreSQL não tem permissão. Use:
```sql
ALTER USER postgres WITH SUPERUSER;
```

---

## ✅ Checklist Final

Verifique se consegue fazer tudo:

- [ ] Acessar pgAdmin (http://localhost:5050)
- [ ] Login funciona (admin@admin.com / admin123)
- [ ] Servidor PostgreSQL conectado
- [ ] Banco CameraAccessDb visível
- [ ] Tabela AccessRules visível
- [ ] Dados de exemplo aparecem (3 linhas)
- [ ] Consegue executar SQL simples (SELECT)
- [ ] Consegue adicionar novo usuário
- [ ] Consegue atualizar dados
- [ ] Consegue deletar usuário
- [ ] Consegue exportar dados

Se tudo marcado ✅ = **Está pronto!**

---

## 🚀 Próximos Passos

1. **API**: Execute `dotnet run`
2. **Testar**: `curl http://localhost:5001/watch/user1`
3. **Monitorar**: Adicione usuários no pgAdmin e teste via API
4. **Integrar**: Conecte MediaMTX para streaming

---

**Dúvidas? Abra um ticket ou consulte [PGADMIN_GUIDE.md](PGADMIN_GUIDE.md)** 💡
