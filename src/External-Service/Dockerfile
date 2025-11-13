## Multi-stage build for React app served by Nginx

# 1) Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Cache dependencies first
COPY package*.json ./
RUN npm ci --no-audit --no-fund

# Copy source and build
COPY . .
RUN npm run build

# 2) Runtime stage (Nginx)
FROM nginx:alpine

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built static files from build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

# Use default nginx entrypoint/cmd
