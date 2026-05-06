# ⚡ CameraAccessAPI - Setup Completo (Resumido)

## 🎯 O que você fez?

```
❌ ANTES
├─ Erro 28P01: autenticação falhou
├─ Sem ferramenta para gerenciar banco
├─ Senhas não sincronizadas
├─ Sem logs estruturados
└─ Difícil debugar

✅ DEPOIS
├─ Docker: PostgreSQL + pgAdmin
├─ pgAdmin: Interface web para gerenciar
├─ Senhas sincronizadas em appsettings
├─ Logs estruturados com CorrelationId
├─ Health check validando conexão
├─ Scripts de setup e debug
└─ Documentação completa
```

---

## 🚀 Começar (2 Opções)

### ⚡ Opção 1: Automática (RECOMENDADO!)
```powershell
.\setup-project.ps1
```
Script faz:
- ✅ Verifica Docker
- ✅ Inicia PostgreSQL + pgAdmin
- ✅ Cria banco e tabelas
- ✅ Abre pgAdmin (opcional)
- ✅ Inicia API (opcional)

### 🔧 Opção 2: Manual
```powershell
docker-compose up -d
```
Depois:
- Abra http://localhost:5050
- Acesse pgAdmin

---

## 📊 Acessar e Usar pgAdmin

### 🔐 Login
```
URL:    http://localhost:5050
Email:  admin@admin.com
Senha:  admin123
```

### 📍 Navegar até Dados
```
Servers
  → CameraAccessDB
    → Databases
      → CameraAccessDb
        → Schemas
          → public
            → Tables
              → AccessRules (Clique aqui!)
```

### 👁️ Ver Dados
Clique direito em **AccessRules** → **View/Edit Data** → **All Rows**

Verá:
```
| user1 | stream_main   | Mon,Tue,Wed,Thu,Fri | 08:00 | 18:00 |
| user2 | stream_backup | Sat,Sun             | 10:00 | 20:00 |
| user3 | stream_main   | Mon,Wed,Fri         | 06:00 | 22:00 |
```

---

## 💾 Executar SQL (Query Tool)

### Abrir Query Tool
1. Clique em **CameraAccessDb** (banco)
2. Menu **Tools** → **Query Tool**

### Exemplos Prontos

#### Listar todos
```sql
SELECT * FROM "AccessRules";
```

#### Buscar um usuário
```sql
SELECT * FROM "AccessRules" WHERE "UserId" = 'user1';
```

#### Contar
```sql
SELECT COUNT(*) FROM "AccessRules";
```

#### Adicionar
```sql
INSERT INTO "AccessRules" ("UserId", "StreamName", "Days", "Start", "End")
VALUES ('user4', 'stream_test', 'Mon,Tue', '09:00:00', '17:00:00');
```

#### Atualizar
```sql
UPDATE "AccessRules" SET "Days" = 'Mon,Tue,Wed,Thu,Fri,Sat,Sun' 
WHERE "UserId" = 'user1';
```

#### Deletar
```sql
DELETE FROM "AccessRules" WHERE "UserId" = 'user4';
```

---

## 🔗 Conectar PostgreSQL em pgAdmin (Primeira Vez)

Se não conectou automaticamente:

1. Clique em **Servers** (esquerda)
2. Clique direito → **New** → **Server**
3. Aba **Connection**:
   ```
   Host:     localhost
   Port:     5432
   User:     postgres
   Password: yourpassword
   ```
4. Clique **Save**

---

## 📋 Credenciais

| Serviço | Host | Porta | User | Senha |
|---------|------|-------|------|-------|
| **PostgreSQL** | localhost | 5432 | postgres | yourpassword |
| **pgAdmin** | localhost | 5050 | admin@admin.com | admin123 |
| **API** | localhost | 5001 | - | - |

---

## 🧪 Testar API

### 1. Executar API
```powershell
dotnet run
```

### 2. Fazer requisição
```bash
curl http://localhost:5001/watch/user1
```

### 3. Resposta Esperada
```json
{
  "hasAccess": true,
  "message": "Acesso permitido",
  "userId": "user1"
}
```

---

## 🔍 Se Tiver Erro

### 1. Rodar diagnóstico
```powershell
.\debug-postgres.ps1
```

### 2. Verificar Docker
```powershell
docker ps | grep camera
```

### 3. Ver logs
```powershell
docker-compose logs postgres
docker-compose logs pgadmin
```

### 4. Reiniciar tudo
```powershell
docker-compose down -v
docker-compose up -d
Start-Sleep -Seconds 20
```

---

## 📚 Documentação

| Documento | O Quê |
|-----------|-------|
| **QUICKSTART.md** | 3 passos para começar |
| **PGADMIN_STEPBYSTEP.md** | Passo a passo com imagens |
| **PGADMIN_GUIDE.md** | Guia completo pgAdmin |
| **TROUBLESHOOTING.md** | Resolver erros |
| **SOLUTION.md** | Análise técnica |
| **README.md** | Visão geral projeto |
| **DOCUMENTATION_INDEX.md** | Índice de tudo |

---

## ✅ Checklist Rápido

- [ ] Docker rodando
- [ ] `docker-compose up -d` executado
- [ ] pgAdmin acessível (http://localhost:5050)
- [ ] Conectado ao PostgreSQL
- [ ] Vendo dados em AccessRules
- [ ] Consegue fazer queries SQL
- [ ] API rodando com `dotnet run`
- [ ] API respondendo em /watch/user1

---

## 🎯 Workflow Diário

```
1️⃣ Inicie Docker
   docker-compose up -d

2️⃣ Abra pgAdmin
   http://localhost:5050
   
3️⃣ Verifique/edite dados
   Vá até AccessRules
   
4️⃣ Execute API
   dotnet run
   
5️⃣ Teste endpoints
   curl http://localhost:5001/watch/user1
   
6️⃣ Monitore logs
   Terminal exibe tudo em tempo real
```

---

## 🚀 Próximos Passos

1. ✅ Setup completo (feito!)
2. 📊 Explorar dados no pgAdmin
3. 🧪 Testar API com curl/Postman
4. 📝 Adicionar novos usuários
5. 🔐 Implementar JWT completo
6. 🎬 Integrar MediaMTX

---

## 💡 Dica Final

**pgAdmin é sua ferramenta de desenvolvimento!**

Use para:
- ✅ Visualizar dados
- ✅ Testar queries
- ✅ Adicionar/editar usuários
- ✅ Monitorar banco
- ✅ Fazer backup

---

**Pronto? Execute: `.\setup-project.ps1`** 🚀

Qualquer dúvida? Veja [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)
