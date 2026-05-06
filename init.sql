-- ========================================
-- Script de Inicialização - CameraAccessAPI
-- ========================================

-- Extensão UUID
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ========================================
-- Tabela de Cameras (Câmeras)
-- ========================================
CREATE TABLE IF NOT EXISTS "Cameras" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) UNIQUE NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Tabela de Regras de Acesso
-- ========================================
CREATE TABLE IF NOT EXISTS "AccessRules" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" VARCHAR(100) NOT NULL,
    stream_id UUID NOT NULL,
    allowed BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_stream
        FOREIGN KEY (stream_id)
        REFERENCES "Streams"(id)
        ON DELETE CASCADE
);

-- ========================================
-- Tabela de Dias da Semana (NORMALIZADO)
-- ========================================
CREATE TABLE IF NOT EXISTS "AccessDays" (
    id SERIAL PRIMARY KEY,
    access_rule_id UUID NOT NULL,
    day_of_week INT NOT NULL, -- 0=Domingo ... 6=Sábado

    CONSTRAINT fk_rule_day
        FOREIGN KEY (access_rule_id)
        REFERENCES "AccessRules"(id)
        ON DELETE CASCADE
);

-- ========================================
-- Tabela de Horários
-- ========================================
CREATE TABLE IF NOT EXISTS "AccessSchedules" (
    id SERIAL PRIMARY KEY,
    access_rule_id UUID NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,

    CONSTRAINT fk_rule_schedule
        FOREIGN KEY (access_rule_id)
        REFERENCES "AccessRules"(id)
        ON DELETE CASCADE
);

-- ========================================
-- Índices (performance)
-- ========================================
CREATE INDEX IF NOT EXISTS idx_rules_user ON "AccessRules"("UserId");
CREATE INDEX IF NOT EXISTS idx_rules_stream ON "AccessRules"(stream_id);
CREATE INDEX IF NOT EXISTS idx_days_rule ON "AccessDays"(access_rule_id);
CREATE INDEX IF NOT EXISTS idx_schedule_rule ON "AccessSchedules"(access_rule_id);

-- ========================================
-- Seeds (dados iniciais)
-- ========================================

-- Cameras
INSERT INTO "Cameras" (name, description) VALUES
('camera_main', 'Câmera principal'),
('camera_backup', 'Câmera backup')
ON CONFLICT (name) DO NOTHING;

-- Regras
INSERT INTO "AccessRules" ("UserId", camera_id, allowed)
SELECT 'user1', c.id, TRUE FROM "Cameras" c WHERE c.name = 'camera_main'
ON CONFLICT DO NOTHING;

INSERT INTO "AccessRules" ("UserId", camera_id, allowed)
SELECT 'user2', c.id, TRUE FROM "Cameras" c WHERE c.name = 'camera_backup'
ON CONFLICT DO NOTHING;

-- Dias
INSERT INTO "AccessDays" (access_rule_id, day_of_week)
SELECT ar.id, d.day
FROM "AccessRules" ar,
     (VALUES (1),(2),(3),(4),(5)) AS d(day)
WHERE ar."UserId" = 'user1'
ON CONFLICT DO NOTHING;

-- Horários
INSERT INTO "AccessSchedules" (access_rule_id, start_time, end_time)
SELECT ar.id, '08:00:00', '18:00:00'
FROM "AccessRules" ar
WHERE ar."UserId" = 'user1'
ON CONFLICT DO NOTHING;

-- ========================================
-- Debug
-- ========================================
SELECT 'Inicialização concluída com sucesso!' as status;