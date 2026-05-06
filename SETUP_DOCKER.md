# Setup Completo com Docker Compose

## Pré-requisitos
- Docker Desktop instalado

## Como Executar

### 1. Subir PostgreSQL + MediaMTX

```bash
docker-compose up -d
```

Aguarde 10-15 segundos para PostgreSQL estar pronto.

### 2. Em outro terminal, iniciar FFmpeg com vídeo fake

```bash
docker run --rm -it jrottenberg/ffmpeg ^
-f lavfi -i testsrc=size=1280x720:rate=30 ^
-f rtsp rtsp://host.docker.internal:8554/test
```

### 3. Em outro terminal, executar a API .NET

```bash
dotnet run
```

A API estará em: `http://localhost:5001`

## Testar Acesso ao Stream

```bash
curl -X GET "http://localhost:5001/watch/user1" ^
-H "Accept: application/json"
```

Resposta esperada:
```json
{
  "stream": "http://localhost:8888/user1?token=eyJ...",
  "expiresInSeconds": 300
}
```

## Parar Tudo

```bash
docker-compose down
```

## Verificar Status

```bash
docker-compose ps
docker logs camera_access_db
docker logs camera_mediamtx
```

## Notas

- PostgreSQL está em `localhost:5432`
- MediaMTX RTSP em `localhost:8554`
- MediaMTX HLS em `localhost:8888`
- A connection string já está configurada para `localhost`