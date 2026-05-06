-- ========================================
-- Schema Completo - CameraAccessAPI
-- VERSÃO CORRIGIDA - Migração Segura
-- ========================================

-- Extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ========================================
-- 1. Tabela Users
-- ========================================
-- Justificativa: Gerenciar usuários do sistema com informações básicas
-- Segurança: Senha hashed, status ativo/inativo
CREATE TABLE IF NOT EXISTS "Users" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(100) UNIQUE NOT NULL, -- Username/ID único
    email VARCHAR(255) UNIQUE,
    full_name VARCHAR(200),
    password_hash VARCHAR(255), -- Para autenticação futura
    is_active BOOLEAN DEFAULT TRUE,
    role VARCHAR(50) DEFAULT 'user', -- admin, user, viewer
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP,
    failed_attempts INTEGER DEFAULT 0,
    locked_until TIMESTAMP
);

-- ========================================
-- 2. Tabela Streams
-- ========================================
-- Justificativa: Gerenciar streams/câmeras disponíveis
-- Escalabilidade: Suporte a metadados e configurações
CREATE TABLE IF NOT EXISTS "Streams" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    stream_name VARCHAR(200) UNIQUE NOT NULL,
    display_name VARCHAR(255),
    url VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    max_concurrent_users INTEGER DEFAULT 10,
    bitrate_kbps INTEGER,
    resolution VARCHAR(50), -- 1080p, 720p, etc.
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 3. Tabela AccessRules (MIGRADA)
-- ========================================
-- Primeiro, fazer backup da tabela existente
CREATE TABLE IF NOT EXISTS "AccessRules_Backup" AS
SELECT * FROM "AccessRules";

-- Adicionar novas colunas à tabela existente (se não existirem)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'AccessRules' AND column_name = 'user_id_uuid') THEN
        ALTER TABLE "AccessRules" ADD COLUMN user_id_uuid UUID;
        ALTER TABLE "AccessRules" ADD COLUMN stream_id_uuid UUID;
        ALTER TABLE "AccessRules" ADD COLUMN days_of_week_new INTEGER[];
        ALTER TABLE "AccessRules" ADD COLUMN is_active_new BOOLEAN DEFAULT TRUE;
        ALTER TABLE "AccessRules" ADD COLUMN priority INTEGER DEFAULT 1;
        ALTER TABLE "AccessRules" ADD COLUMN created_by UUID;
    END IF;
END $$;

-- Migrar dados para novas colunas
UPDATE "AccessRules"
SET days_of_week_new = CASE
    WHEN "Days" = 'Mon,Tue,Wed,Thu,Fri' THEN ARRAY[1,2,3,4,5]
    WHEN "Days" = 'Sat,Sun' THEN ARRAY[6,0]
    ELSE ARRAY[1,2,3,4,5] -- default para weekdays
END;

-- ========================================
-- 4. Tabela AuditLogs
-- ========================================
-- Justificativa: Logs de auditoria para segurança e compliance
-- NOTA: Removido particionamento para simplificar - pode ser adicionado depois
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id),
    stream_id UUID REFERENCES "Streams"(id),
    action VARCHAR(100) NOT NULL, -- access_granted, access_denied, login, logout, etc.
    ip_address INET,
    user_agent TEXT,
    jwt_token_id VARCHAR(255), -- Para rastrear tokens
    details JSONB, -- Metadados adicionais
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    correlation_id VARCHAR(255) -- Para rastrear requisições
);

-- ========================================
-- 5. Tabela FailedAttempts
-- ========================================
-- Justificativa: Controle de tentativas falhidas para rate limiting
CREATE TABLE IF NOT EXISTS "FailedAttempts" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id) ON DELETE CASCADE,
    ip_address INET NOT NULL,
    attempt_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reason VARCHAR(255) -- access_denied, invalid_token, etc.
);

-- ========================================
-- 6. Tabela UserBlocks
-- ========================================
-- Justificativa: Bloqueios temporários ou permanentes de usuários
CREATE TABLE IF NOT EXISTS "UserBlocks" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    blocked_by UUID REFERENCES "Users"(id),
    reason VARCHAR(500),
    block_type VARCHAR(50) DEFAULT 'temporary', -- temporary, permanent
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP, -- NULL para permanente
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 7. Tabela ActiveSessions (Opcional)
-- ========================================
-- Justificativa: Rastrear sessões ativas para invalidação
CREATE TABLE IF NOT EXISTS "ActiveSessions" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    stream_id UUID REFERENCES "Streams"(id),
    jwt_token_id VARCHAR(255) UNIQUE NOT NULL,
    ip_address INET,
    user_agent TEXT,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 5. Tabela FailedAttempts
-- ========================================
-- Justificativa: Controle de tentativas falhidas para rate limiting
CREATE TABLE IF NOT EXISTS "FailedAttempts" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id) ON DELETE CASCADE,
    ip_address INET NOT NULL,
    attempt_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reason VARCHAR(255) -- access_denied, invalid_token, etc.
);

-- ========================================
-- 6. Tabela UserBlocks
-- ========================================
-- Justificativa: Bloqueios temporários ou permanentes de usuários
CREATE TABLE IF NOT EXISTS "UserBlocks" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    blocked_by UUID REFERENCES "Users"(id),
    reason VARCHAR(500),
    block_type VARCHAR(50) DEFAULT 'temporary', -- temporary, permanent
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP, -- NULL para permanente
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 7. Tabela ActiveSessions (Opcional)
-- ========================================
-- Justificativa: Rastrear sessões ativas para invalidação
CREATE TABLE IF NOT EXISTS "ActiveSessions" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    stream_id UUID REFERENCES "Streams"(id),
    jwt_token_id VARCHAR(255) UNIQUE NOT NULL,
    ip_address INET,
    user_agent TEXT,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- ÍNDICES PARA PERFORMANCE
-- ========================================

-- Users
CREATE INDEX IF NOT EXISTS idx_users_user_id ON "Users"(user_id);
CREATE INDEX IF NOT EXISTS idx_users_email ON "Users"(email);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON "Users"(is_active);
CREATE INDEX IF NOT EXISTS idx_users_failed_attempts ON "Users"(failed_attempts);

-- Streams
CREATE INDEX IF NOT EXISTS idx_streams_stream_name ON "Streams"(stream_name);
CREATE INDEX IF NOT EXISTS idx_streams_is_active ON "Streams"(is_active);

-- AccessRules (usando campos antigos para compatibilidade)
CREATE INDEX IF NOT EXISTS idx_access_rules_user_id ON "AccessRules"("UserId");
CREATE INDEX IF NOT EXISTS idx_access_rules_stream_name ON "AccessRules"("StreamName");

-- Novos índices para campos migrados
CREATE INDEX IF NOT EXISTS idx_access_rules_user_uuid ON "AccessRules"(user_id_uuid);
CREATE INDEX IF NOT EXISTS idx_access_rules_stream_uuid ON "AccessRules"(stream_id_uuid);
CREATE INDEX IF NOT EXISTS idx_access_rules_active_new ON "AccessRules"(is_active_new);

-- AuditLogs
CREATE INDEX IF NOT EXISTS idx_audit_logs_user ON "AuditLogs"(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_stream ON "AuditLogs"(stream_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON "AuditLogs"(action);
CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON "AuditLogs"(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_correlation_id ON "AuditLogs"(correlation_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_details ON "AuditLogs" USING GIN(details);

-- FailedAttempts
CREATE INDEX IF NOT EXISTS idx_failed_attempts_user_ip ON "FailedAttempts"(user_id, ip_address);
CREATE INDEX IF NOT EXISTS idx_failed_attempts_time ON "FailedAttempts"(attempt_time DESC);

-- UserBlocks
CREATE INDEX IF NOT EXISTS idx_user_blocks_user ON "UserBlocks"(user_id);
CREATE INDEX IF NOT EXISTS idx_user_blocks_active ON "UserBlocks"(is_active);
CREATE INDEX IF NOT EXISTS idx_user_blocks_end_time ON "UserBlocks"(end_time);

-- ActiveSessions
CREATE INDEX IF NOT EXISTS idx_active_sessions_user ON "ActiveSessions"(user_id);
CREATE INDEX IF NOT EXISTS idx_active_sessions_token ON "ActiveSessions"(jwt_token_id);
CREATE INDEX IF NOT EXISTS idx_active_sessions_expires ON "ActiveSessions"(expires_at);

-- ========================================
-- CONSTRAINTS E TRIGGERS
-- ========================================

-- Constraint: Role válido
ALTER TABLE "Users" ADD CONSTRAINT IF NOT EXISTS chk_users_role
    CHECK (role IN ('admin', 'user', 'viewer'));

-- Constraint: Block type válido
ALTER TABLE "UserBlocks" ADD CONSTRAINT IF NOT EXISTS chk_user_blocks_type
    CHECK (block_type IN ('temporary', 'permanent'));

-- Trigger: Atualizar updated_at automaticamente
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER IF NOT EXISTS update_users_updated_at BEFORE UPDATE ON "Users"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER IF NOT EXISTS update_streams_updated_at BEFORE UPDATE ON "Streams"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ========================================
-- DADOS DE EXEMPLO
-- ========================================

-- Usuários de exemplo
INSERT INTO "Users" (user_id, email, full_name, role) VALUES
('admin', 'admin@cameraapi.com', 'Administrator', 'admin'),
('user1', 'user1@company.com', 'João Silva', 'user'),
('user2', 'user2@company.com', 'Maria Santos', 'user'),
('viewer1', 'viewer1@company.com', 'Pedro Costa', 'viewer')
ON CONFLICT (user_id) DO NOTHING;

-- Streams de exemplo
INSERT INTO "Streams" (stream_name, display_name, url, resolution) VALUES
('stream_main', 'Câmera Principal', 'rtmp://localhost:1935/live/stream_main', '1080p'),
('stream_backup', 'Câmera Backup', 'rtmp://localhost:1935/live/stream_backup', '720p'),
('stream_entrance', 'Entrada Principal', 'rtmp://localhost:1935/live/stream_entrance', '1080p')
ON CONFLICT (stream_name) DO NOTHING;

-- ========================================
-- VIEWS PARA FACILITAR CONSULTAS
-- ========================================

-- View: Regras de acesso com nomes (compatível com estrutura atual)
CREATE OR REPLACE VIEW "AccessRulesView" AS
SELECT
    ar.id,
    ar."UserId" as user_id_string,
    COALESCE(u.full_name, ar."UserId") as user_name,
    ar."StreamName" as stream_name_string,
    COALESCE(s.display_name, ar."StreamName") as stream_display_name,
    ar."Days" as days_string,
    ar."Start" as start_time,
    ar."End" as end_time,
    ar.created_at,
    ar.updated_at
FROM "AccessRules" ar
LEFT JOIN "Users" u ON ar."UserId" = u.user_id
LEFT JOIN "Streams" s ON ar."StreamName" = s.stream_name;

-- ========================================
-- FUNÇÕES ÚTEIS
-- ========================================

-- Função: Verificar se usuário tem acesso a stream (versão compatível)
CREATE OR REPLACE FUNCTION check_user_access(p_user_id VARCHAR(100), p_stream_name VARCHAR(200), p_current_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP)
RETURNS BOOLEAN AS $$
DECLARE
    v_day_of_week INTEGER;
    v_current_time TIME;
    v_days_string VARCHAR(50);
    v_start_time TIME;
    v_end_time TIME;
    v_has_access BOOLEAN := FALSE;
BEGIN
    -- Verificar se usuário está bloqueado
    IF EXISTS (
        SELECT 1 FROM "UserBlocks" ub
        JOIN "Users" u ON ub.user_id = u.id
        WHERE u.user_id = p_user_id
        AND ub.is_active = TRUE
        AND (ub.end_time IS NULL OR ub.end_time > p_current_time)
    ) THEN
        RETURN FALSE;
    END IF;

    -- Buscar regra de acesso
    SELECT "Days", "Start", "End" INTO v_days_string, v_start_time, v_end_time
    FROM "AccessRules"
    WHERE "UserId" = p_user_id AND "StreamName" = p_stream_name;

    IF NOT FOUND THEN
        RETURN FALSE;
    END IF;

    -- Calcular dia da semana e hora atual
    v_day_of_week := EXTRACT(DOW FROM p_current_time);
    v_current_time := p_current_time::TIME;

    -- Verificar se o dia atual está na lista de dias permitidos
    v_has_access := CASE
        WHEN v_days_string = 'Mon,Tue,Wed,Thu,Fri' AND v_day_of_week BETWEEN 1 AND 5 THEN TRUE
        WHEN v_days_string = 'Sat,Sun' AND v_day_of_week IN (0, 6) THEN TRUE
        WHEN v_days_string LIKE '%' || CASE v_day_of_week
            WHEN 0 THEN 'Sun'
            WHEN 1 THEN 'Mon'
            WHEN 2 THEN 'Tue'
            WHEN 3 THEN 'Wed'
            WHEN 4 THEN 'Thu'
            WHEN 5 THEN 'Fri'
            WHEN 6 THEN 'Sat'
        END || '%' THEN TRUE
        ELSE FALSE
    END;

    -- Verificar horário
    IF v_has_access THEN
        v_has_access := v_current_time BETWEEN v_start_time AND v_end_time;
    END IF;

    RETURN v_has_access;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- POLÍTICAS DE SEGURANÇA (RLS) - Opcional
-- ========================================

-- Habilitar RLS nas tabelas sensíveis (desabilitado por padrão para compatibilidade)
-- ALTER TABLE "AuditLogs" ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE "FailedAttempts" ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE "UserBlocks" ENABLE ROW LEVEL SECURITY;

-- ========================================
-- FINALIZAÇÃO
-- ========================================

-- Verificar tabelas criadas
SELECT 'Schema migrado com sucesso!' as status;
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
ORDER BY table_name;