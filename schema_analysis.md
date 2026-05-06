# 📋 ANÁLISE COMPLETA - CameraAccessAPI Database Schema

## 🎯 OBJETIVO
Fornecer um schema de banco de dados completo e seguro para suprir todas as demandas do sistema CameraAccessAPI, incluindo controle de acesso, auditoria, segurança e escalabilidade.

## 📊 TABELAS IMPLEMENTADAS

### 1. **Users** - Gerenciamento de Usuários
- **Campos**: id, user_id, email, full_name, password_hash, is_active, role, timestamps, failed_attempts, locked_until
- **Justificativa**: Centralizar informações de usuários para autenticação futura e controle de acesso
- **Segurança**: Password hashing, controle de tentativas falhidas, bloqueios automáticos

### 2. **Streams** - Gerenciamento de Streams/Câmeras
- **Campos**: id, stream_name, display_name, url, is_active, max_concurrent_users, bitrate, resolution, timestamps
- **Justificativa**: Gerenciar metadados das streams para controle de qualidade e limitações
- **Escalabilidade**: Suporte a configurações por stream

### 3. **AccessRules** - Regras de Acesso
- **Campos**: id, user_id (FK), stream_id (FK), days_of_week (array), start_time, end_time, is_active, priority, timestamps
- **Justificativa**: Definir regras de acesso por horário e dia da semana
- **Melhoria**: Foreign keys para integridade, array para dias flexível, prioridade para conflitos

### 4. **AuditLogs** - Logs de Auditoria
- **Campos**: id, user_id (FK), stream_id (FK), action, ip_address, user_agent, jwt_token_id, details (JSONB), timestamp, correlation_id
- **Justificativa**: Rastreamento completo de todas as ações para compliance e segurança
- **Escalabilidade**: Particionamento mensal, índices GIN para JSONB

### 5. **FailedAttempts** - Controle de Tentativas Falhidas
- **Campos**: id, user_id (FK), ip_address, attempt_time, reason
- **Justificativa**: Implementar rate limiting e detecção de ataques de força bruta
- **Segurança**: Bloqueio automático após limite de tentativas

### 6. **UserBlocks** - Bloqueios de Usuários
- **Campos**: id, user_id (FK), blocked_by (FK), reason, block_type, start_time, end_time, is_active
- **Justificativa**: Bloqueios temporários ou permanentes por segurança
- **Flexibilidade**: Tipos diferentes de bloqueio com durações variáveis

### 7. **ActiveSessions** - Sessões Ativas (Opcional)
- **Campos**: id, user_id (FK), stream_id (FK), jwt_token_id, ip_address, user_agent, expires_at, last_activity
- **Justificativa**: Rastreamento de sessões para invalidação e monitoramento
- **Segurança**: Permite logout forçado e detecção de uso simultâneo

## 🔍 ÍNDICES E PERFORMANCE

### Índices Implementados:
- **B-tree**: Para buscas por ID, timestamps, strings únicas
- **GIN**: Para arrays (days_of_week) e JSONB (details)
- **Compostos**: Para combinações frequentes (user_id + stream_id)

### Estratégias de Performance:
- **Particionamento**: AuditLogs particionado mensalmente
- **Índices Parciais**: Para registros ativos (is_active = true)
- **Views Materializadas**: Para estatísticas frequentes (se necessário)

## 🔒 CONSIDERAÇÕES DE SEGURANÇA

### 1. **Autenticação e Autorização**
- JWT tokens com expiração curta (5 minutos)
- Controle de tentativas falhidas com bloqueio automático
- Roles de usuário (admin, user, viewer)

### 2. **Auditoria e Compliance**
- Logs de todas as ações com contexto completo
- Correlation ID para rastreamento de requisições
- IP address e User-Agent registrados

### 3. **Proteção contra Ataques**
- Rate limiting por IP e usuário
- Bloqueios automáticos após tentativas falhidas
- Validação de entrada em todas as operações

### 4. **Row Level Security (RLS)**
- Políticas implementadas para restringir acesso aos dados
- Usuários veem apenas seus próprios registros (exceto admins)

## ⚡ FUNCIONALIDADES AVANÇADAS

### Triggers Automáticos:
- **updated_at**: Atualização automática de timestamps
- **Auditoria**: Logs automáticos de mudanças em AccessRules

### Funções Utilitárias:
- **check_user_access()**: Verificação de acesso em tempo real
- **log_failed_attempt()**: Registro de tentativas falhidas com bloqueio automático

### Views para Consultas:
- **AccessRulesView**: Regras com nomes de usuário e stream
- **AccessStatsView**: Estatísticas de acesso por usuário

## 📈 ESCALABILIDADE

### Particionamento:
- AuditLogs particionado por mês para melhor performance
- Possibilidade de particionar FailedAttempts por data

### Otimizações:
- Índices apropriados para consultas frequentes
- JSONB para metadados flexíveis
- Arrays para dados relacionais simples

## 🔧 MANUTENIBILIDADE

### Constraints:
- Foreign keys para integridade referencial
- Checks para validação de dados (horários, roles, etc.)
- Unique constraints para evitar duplicatas

### Estrutura Clara:
- Nomes descritivos de tabelas e campos
- Comentários em SQL para documentação
- Views para simplificar consultas complexas

## 🚀 MIGRAÇÃO DO SCHEMA ATUAL

Para migrar do schema atual:

1. **Backup**: Fazer backup completo dos dados existentes
2. **Criar novas tabelas**: Executar o script completo
3. **Migrar dados**:
   ```sql
   -- Migrar usuários (se não existirem)
   INSERT INTO "Users" (user_id, is_active, role)
   SELECT DISTINCT "UserId", true, 'user' FROM "AccessRules"
   ON CONFLICT (user_id) DO NOTHING;

   -- Migrar streams
   INSERT INTO "Streams" (stream_name, is_active)
   SELECT DISTINCT "StreamName", true FROM "AccessRules"
   ON CONFLICT (stream_name) DO NOTHING;

   -- Migrar regras de acesso
   INSERT INTO "AccessRules" (user_id, stream_id, days_of_week, start_time, end_time)
   SELECT u.id, s.id,
          CASE
              WHEN "Days" = 'Mon,Tue,Wed,Thu,Fri' THEN ARRAY[1,2,3,4,5]
              WHEN "Days" = 'Sat,Sun' THEN ARRAY[6,0]
              ELSE ARRAY[1,2,3,4,5] -- default
          END,
          "Start", "End"
   FROM "AccessRules" ar
   JOIN "Users" u ON ar."UserId" = u.user_id
   JOIN "Streams" s ON ar."StreamName" = s.stream_name;
   ```
4. **Atualizar aplicação**: Modificar entidades e repositórios para usar novos tipos
5. **Testar**: Validar todas as funcionalidades

## 📋 CONSIDERAÇÕES FINAIS

Este schema fornece uma base sólida e escalável para o CameraAccessAPI, cobrindo:

- ✅ **Funcionalidades**: Controle de acesso completo com horários e dias
- ✅ **Segurança**: Auditoria, rate limiting, bloqueios automáticos
- ✅ **Escalabilidade**: Índices otimizados, particionamento
- ✅ **Manutenibilidade**: Constraints, triggers, estrutura clara
- ✅ **Performance**: Índices estratégicos, views otimizadas

O schema está preparado para crescimento futuro e pode ser facilmente estendido com novas funcionalidades conforme necessário.