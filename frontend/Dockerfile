# ====================================================================
# Dockerfile Multi-Stage para Frontend (SvelteKit + Nginx)
# Compilação estática com adapter-static e servidor Nginx ultraleve (~25MB)
# ====================================================================

# 1. Estágio de Build
FROM node:20-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Injeta a URL base da API se passada por build arg (padrão relativo /api/v1)
ARG VITE_API_URL=/api/v1
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

# 2. Estágio de Servidor Web Estático (Nginx Alpine)
FROM nginx:alpine AS runner
WORKDIR /usr/share/nginx/html

# Limpa arquivos padrão do Nginx
RUN rm -rf ./*

# Copia build estático do SvelteKit
COPY --from=builder /app/build ./

# Copia configuração do Nginx com suporte a SPA fallback
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
