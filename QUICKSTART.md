# 🚀 Quick Start - CameraAccessAPI

## ⚡ Setup em 2 Comandos

### 1️⃣ Executar o script de setup (Recomendado)
```powershell
.\setup-project.ps1
```

O script vai:
- ✅ Verificar Docker
- ✅ Iniciar PostgreSQL + pgAdmin
- ✅ Criar banco e tabelas automaticamente
- ✅ Abrir pgAdmin no navegador (opcional)
- ✅ Compilar e executar a API (opcional)

---

## 🔄 Ou Fazer Manualmente

### 1. Inicie o Docker
```powershell
docker-compose up -d
```

### 2. Abra pgAdmin
```
http://localhost:5050
Email: admin@admin.com
Senha: admin123
```

### 3. Execute a API
```powershell
dotnet run
```

### 4. Teste
```bash
curl http://localhost:5001/watch/user1
```

---

## 📊 Acessar Dados via pgAdmin

1. Abra http://localhost:5050
2. Login: `admin@admin.com` / `admin123`
3. Procure por **CameraAccessDB** → Conecte (se não estiver conectado)
4. Vá em: **Databases** → **CameraAccessDb** → **Schemas** → **public** → **Tables** → **AccessRules**
5. Clique com botão direito → **View/Edit Data** → **All Rows**

---

## 📋 O que foi criado automaticamente?

| Item | Descrição |
|------|-----------|
| **PostgreSQL** | Banco de dados em container (porta 5432) |
| **pgAdmin** | Gerenciador web do banco (porta 5050) |
| **CameraAccessDb** | Banco chamado "CameraAccessDb" |
| **AccessRules** | Tabela com 3 usuários de exemplo |
| **Dados iniciais** | user1, user2, user3 com horários de acesso |

---

## 🔐 Credenciais

| Serviço | Host | Porta | User | Senha |
|---------|------|-------|------|-------|
| PostgreSQL | localhost | 5432 | postgres | yourpassword |
| pgAdmin | localhost | 5050 | admin@admin.com | admin123 |
| API | localhost | 5001 | - | - |

---

## ✅ Checklist Rápido

- [ ] Docker rodando
- [ ] `docker-compose up -d` executado
- [ ] pgAdmin acessível em http://localhost:5050
- [ ] Conectado ao banco em pgAdmin
- [ ] Tabela AccessRules visível com dados
- [ ] API rodando com `dotnet run`
- [ ] API respondendo em http://localhost:5001/watch/user1

---

## 🎯 Próximos Passos

1. **Explorar dados** no pgAdmin
2. **Adicionar novos usuários** via SQL ou API
3. **Monitorar logs** da aplicação
4. **Consultar documentação** em [PGADMIN_GUIDE.md](PGADMIN_GUIDE.md)

---

**Dúvidas?** Veja [SOLUTION.md](SOLUTION.md) ou [PGADMIN_GUIDE.md](PGADMIN_GUIDE.md) 💡
