-- ========================================
-- CameraAccessAPI - Schema Completo v3.0
-- Data: 2026-05-04
-- Descrição: Schema completo e seguro para CameraAccessAPI
-- ========================================

-- ========================================
-- 1. EXTENSÕES NECESSÁRIAS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ========================================
-- 2. TABELAS PRINCIPAIS
-- ========================================

-- Tabela de Usuários
CREATE TABLE IF NOT EXISTS "Users" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_username_length CHECK (char_length(username) >= 3),
    CONSTRAINT chk_email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

-- Tabela de Streams/Câmeras
CREATE TABLE IF NOT EXISTS "Streams" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) UNIQUE NOT NULL,
    description TEXT,
    url VARCHAR(500),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_stream_name_length CHECK (char_length(name) >= 1)
);

-- Tabela de Regras de Acesso (MIGRADA)
CREATE TABLE IF NOT EXISTS "AccessRules" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    stream_id UUID NOT NULL REFERENCES "Streams"(id) ON DELETE CASCADE,
    days_of_week INTEGER[] NOT NULL, -- Array de 0-6 (Domingo=0, Sábado=6)
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_days_range CHECK (array_length(days_of_week, 1) > 0 AND
                                    array_length(days_of_week, 1) <= 7),
    CONSTRAINT chk_time_range CHECK (start_time < end_time),
    CONSTRAINT chk_days_values CHECK (days_of_week <@ ARRAY[0,1,2,3,4,5,6])
);

-- ========================================
-- 3. TABELAS DE SEGURANÇA E AUDITORIA
-- ========================================

-- Logs de Auditoria
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id) ON DELETE SET NULL,
    action VARCHAR(50) NOT NULL, -- 'LOGIN', 'LOGOUT', 'ACCESS_GRANTED', 'ACCESS_DENIED', etc.
    resource VARCHAR(100), -- 'stream', 'user', etc.
    resource_id UUID,
    ip_address INET,
    user_agent TEXT,
    details JSONB,
    success BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tentativas de Acesso Falhadas
CREATE TABLE IF NOT EXISTS "FailedAttempts" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL,
    ip_address INET NOT NULL,
    user_agent TEXT,
    reason VARCHAR(100), -- 'INVALID_CREDENTIALS', 'OUTSIDE_TIME', etc.
    attempt_count INTEGER DEFAULT 1,
    first_attempt_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_attempt_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_blocked BOOLEAN DEFAULT false,
    blocked_until TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Bloqueios de Usuário
CREATE TABLE IF NOT EXISTS "UserBlocks" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES "Users"(id) ON DELETE CASCADE,
    reason VARCHAR(100) NOT NULL,
    blocked_by UUID REFERENCES "Users"(id),
    blocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Sessões Ativas
CREATE TABLE IF NOT EXISTS "ActiveSessions" (
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

-- ========================================
-- 4. ÍNDICES PARA PERFORMANCE
-- ========================================

-- Índices para Users
CREATE INDEX IF NOT EXISTS idx_users_username ON "Users"(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON "Users"(email);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON "Users"(is_active);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON "Users"(created_at);

-- Índices para Streams
CREATE INDEX IF NOT EXISTS idx_streams_name ON "Streams"(name);
CREATE INDEX IF NOT EXISTS idx_streams_is_active ON "Streams"(is_active);

-- Índices para AccessRules
CREATE INDEX IF NOT EXISTS idx_access_rules_user_id ON "AccessRules"(user_id);
CREATE INDEX IF NOT EXISTS idx_access_rules_stream_id ON "AccessRules"(stream_id);
CREATE INDEX IF NOT EXISTS idx_access_rules_active ON "AccessRules"(is_active);
CREATE INDEX IF NOT EXISTS idx_access_rules_days ON "AccessRules" USING GIN(days_of_week);
CREATE INDEX IF NOT EXISTS idx_access_rules_times ON "AccessRules"(start_time, end_time);

-- Índices para AuditLogs
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON "AuditLogs"(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON "AuditLogs"(action);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON "AuditLogs"(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_logs_resource ON "AuditLogs"(resource, resource_id);

-- Índices para FailedAttempts
CREATE INDEX IF NOT EXISTS idx_failed_attempts_username ON "FailedAttempts"(username);
CREATE INDEX IF NOT EXISTS idx_failed_attempts_ip ON "FailedAttempts"(ip_address);
CREATE INDEX IF NOT EXISTS idx_failed_attempts_last_attempt ON "FailedAttempts"(last_attempt_at);
CREATE INDEX IF NOT EXISTS idx_failed_attempts_blocked ON "FailedAttempts"(is_blocked);

-- Índices para UserBlocks
CREATE INDEX IF NOT EXISTS idx_user_blocks_user_id ON "UserBlocks"(user_id);
CREATE INDEX IF NOT EXISTS idx_user_blocks_active ON "UserBlocks"(is_active);
CREATE INDEX IF NOT EXISTS idx_user_blocks_expires_at ON "UserBlocks"(expires_at);

-- Índices para ActiveSessions
CREATE INDEX IF NOT EXISTS idx_active_sessions_user_id ON "ActiveSessions"(user_id);
CREATE INDEX IF NOT EXISTS idx_active_sessions_token_hash ON "ActiveSessions"(token_hash);
CREATE INDEX IF NOT EXISTS idx_active_sessions_expires_at ON "ActiveSessions"(expires_at);
CREATE INDEX IF NOT EXISTS idx_active_sessions_last_activity ON "ActiveSessions"(last_activity);

-- ========================================
-- 5. FUNCTIONS ÚTEIS
-- ========================================

-- Função para verificar acesso do usuário
CREATE OR REPLACE FUNCTION check_user_access(p_user_id UUID, p_stream_name VARCHAR, p_current_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP)
RETURNS BOOLEAN AS $$
DECLARE
    v_day_of_week INTEGER;
    v_current_time TIME;
    v_access_count INTEGER;
BEGIN
    -- Extrair dia da semana (0=Domingo, 6=Sábado)
    v_day_of_week := EXTRACT(DOW FROM p_current_time);
    v_current_time := p_current_time::TIME;

    -- Verificar se usuário tem acesso
    SELECT COUNT(*)
    INTO v_access_count
    FROM "AccessRules" ar
    JOIN "Users" u ON ar.user_id = u.id
    JOIN "Streams" s ON ar.stream_id = s.id
    WHERE u.username = (SELECT username FROM "Users" WHERE id = p_user_id)
      AND s.name = p_stream_name
      AND ar.is_active = true
      AND u.is_active = true
      AND s.is_active = true
      AND v_day_of_week = ANY(ar.days_of_week)
      AND v_current_time BETWEEN ar.start_time AND ar.end_time
      AND NOT EXISTS (
          SELECT 1 FROM "UserBlocks"
          WHERE user_id = p_user_id
            AND is_active = true
            AND (expires_at IS NULL OR expires_at > CURRENT_TIMESTAMP)
      );

    RETURN v_access_count > 0;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Função para registrar tentativa falhada
CREATE OR REPLACE FUNCTION record_failed_attempt(p_username VARCHAR, p_ip INET, p_reason VARCHAR)
RETURNS VOID AS $$
DECLARE
    v_existing_record "FailedAttempts"%ROWTYPE;
BEGIN
    -- Buscar registro existente
    SELECT * INTO v_existing_record
    FROM "FailedAttempts"
    WHERE username = p_username AND ip_address = p_ip
    ORDER BY last_attempt_at DESC
    LIMIT 1;

    IF FOUND THEN
        -- Atualizar registro existente
        UPDATE "FailedAttempts"
        SET attempt_count = attempt_count + 1,
            last_attempt_at = CURRENT_TIMESTAMP,
            is_blocked = CASE WHEN attempt_count >= 5 THEN true ELSE false END,
            blocked_until = CASE WHEN attempt_count >= 5 THEN CURRENT_TIMESTAMP + INTERVAL '30 minutes' ELSE NULL END
        WHERE id = v_existing_record.id;
    ELSE
        -- Criar novo registro
        INSERT INTO "FailedAttempts" (username, ip_address, reason)
        VALUES (p_username, p_ip, p_reason);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- 6. TRIGGERS PARA AUDITORIA
-- ========================================

-- Trigger para atualizar updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Aplicar trigger nas tabelas principais
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON "Users"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_streams_updated_at BEFORE UPDATE ON "Streams"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_access_rules_updated_at BEFORE UPDATE ON "AccessRules"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Trigger para auditoria de AccessRules
CREATE OR REPLACE FUNCTION audit_access_rules_changes()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO "AuditLogs" (action, resource, resource_id, details)
        VALUES ('ACCESS_RULE_CREATED', 'access_rule', NEW.id,
                jsonb_build_object('user_id', NEW.user_id, 'stream_id', NEW.stream_id));
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO "AuditLogs" (action, resource, resource_id, details)
        VALUES ('ACCESS_RULE_UPDATED', 'access_rule', NEW.id,
                jsonb_build_object('old', row_to_json(OLD), 'new', row_to_json(NEW)));
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO "AuditLogs" (action, resource, resource_id, details)
        VALUES ('ACCESS_RULE_DELETED', 'access_rule', OLD.id, row_to_json(OLD));
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_access_rules_trigger
    AFTER INSERT OR UPDATE OR DELETE ON "AccessRules"
    FOR EACH ROW EXECUTE FUNCTION audit_access_rules_changes();

-- ========================================
-- 7. VIEWS PARA CONSULTAS SIMPLIFICADAS
-- ========================================

-- View para regras de acesso com nomes
CREATE OR REPLACE VIEW "AccessRulesView" AS
SELECT
    ar.id,
    u.username,
    s.name as stream_name,
    ar.days_of_week,
    ar.start_time,
    ar.end_time,
    ar.is_active,
    ar.created_at,
    ar.updated_at
FROM "AccessRules" ar
JOIN "Users" u ON ar.user_id = u.id
JOIN "Streams" s ON ar.stream_id = s.id;

-- View para estatísticas de segurança
CREATE OR REPLACE VIEW "SecurityStatsView" AS
SELECT
    (SELECT COUNT(*) FROM "FailedAttempts" WHERE is_blocked = true) as blocked_attempts,
    (SELECT COUNT(*) FROM "UserBlocks" WHERE is_active = true) as active_blocks,
    (SELECT COUNT(*) FROM "ActiveSessions" WHERE expires_at > CURRENT_TIMESTAMP) as active_sessions,
    (SELECT COUNT(*) FROM "AuditLogs" WHERE created_at >= CURRENT_TIMESTAMP - INTERVAL '24 hours') as recent_logs;

-- ========================================
-- 8. DADOS DE EXEMPLO (SEGUROS)
-- ========================================

-- Inserir usuários de exemplo
INSERT INTO "Users" (username, email) VALUES
('admin', 'admin@cameraaccess.com'),
('user1', 'user1@empresa.com'),
('user2', 'user2@empresa.com'),
('user3', 'user3@empresa.com')
ON CONFLICT (username) DO NOTHING;

-- Inserir streams de exemplo
INSERT INTO "Streams" (name, description) VALUES
('stream_main', 'Câmera principal da entrada'),
('stream_backup', 'Câmera de backup do estacionamento'),
('stream_internal', 'Câmera interna do escritório')
ON CONFLICT (name) DO NOTHING;

-- Inserir regras de acesso de exemplo (usando IDs)
INSERT INTO "AccessRules" (user_id, stream_id, days_of_week, start_time, end_time)
SELECT
    u.id, s.id,
    CASE u.username
        WHEN 'user1' THEN ARRAY[1,2,3,4,5] -- Seg-Sex
        WHEN 'user2' THEN ARRAY[0,6] -- Fim de semana
        WHEN 'user3' THEN ARRAY[1,3,5] -- Seg-Qua-Sex
        ELSE ARRAY[1,2,3,4,5] -- Default
    END,
    CASE u.username
        WHEN 'user1' THEN '08:00:00'::TIME
        WHEN 'user2' THEN '10:00:00'::TIME
        WHEN 'user3' THEN '06:00:00'::TIME
        ELSE '09:00:00'::TIME
    END,
    CASE u.username
        WHEN 'user1' THEN '18:00:00'::TIME
        WHEN 'user2' THEN '20:00:00'::TIME
        WHEN 'user3' THEN '22:00:00'::TIME
        ELSE '17:00:00'::TIME
    END
FROM "Users" u
CROSS JOIN "Streams" s
WHERE u.username IN ('user1', 'user2', 'user3')
  AND s.name = 'stream_main'
ON CONFLICT DO NOTHING;

-- ========================================
-- 9. PERMISSÕES DE SEGURANÇA
-- ========================================

-- Criar role para aplicação (se necessário)
-- DO $$
-- BEGIN
--     IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'camera_app') THEN
--         CREATE ROLE camera_app LOGIN PASSWORD 'secure_password_here';
--         GRANT CONNECT ON DATABASE "CameraAccessDb" TO camera_app;
--         GRANT USAGE ON SCHEMA public TO camera_app;
--         GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO camera_app;
--         GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO camera_app;
--     END IF;
-- END $$;

-- ========================================
-- 10. VALIDAÇÃO FINAL
-- ========================================

-- Verificar se tudo foi criado corretamente
DO $$
DECLARE
    table_count INTEGER;
    index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO table_count
    FROM information_schema.tables
    WHERE table_schema = 'public'
      AND table_name IN ('Users', 'Streams', 'AccessRules', 'AuditLogs', 'FailedAttempts', 'UserBlocks', 'ActiveSessions');

    SELECT COUNT(*) INTO index_count
    FROM pg_indexes
    WHERE schemaname = 'public'
      AND tablename IN ('Users', 'Streams', 'AccessRules', 'AuditLogs', 'FailedAttempts', 'UserBlocks', 'ActiveSessions');

    RAISE NOTICE 'Schema CameraAccessAPI criado com sucesso!';
    RAISE NOTICE 'Tabelas criadas: %', table_count;
    RAISE NOTICE 'Índices criados: %', index_count;
    RAISE NOTICE 'Functions criadas: check_user_access, record_failed_attempt';
    RAISE NOTICE 'Triggers criados: update_updated_at, audit_access_rules';
    RAISE NOTICE 'Views criadas: AccessRulesView, SecurityStatsView';
END $$;

-- ========================================
-- FIM DO SCRIPT
-- ========================================