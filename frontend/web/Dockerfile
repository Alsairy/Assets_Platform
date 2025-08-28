# Build
FROM node:20-alpine AS build
WORKDIR /app
<<<<<<< Updated upstream

# Better cache: copy only manifests first, install, then copy src
COPY package*.json ./
ENV NODE_ENV=production
RUN npm ci

||||||| constructed merge base
COPY package.json package-lock.json* pnpm-lock.yaml* yarn.lock* ./
RUN if [ -f package-lock.json ]; then npm ci;     elif [ -f pnpm-lock.yaml ]; then npm i -g pnpm && pnpm i;     elif [ -f yarn.lock ]; then yarn;     else npm i; fi
=======

# Cache-friendly install
COPY package*.json ./
RUN npm ci

# Copy source and build with production optimizations
>>>>>>> Stashed changes
COPY . .
<<<<<<< Updated upstream
RUN npm run build

# Serve
||||||| constructed merge base
RUN npm run build
=======
RUN NODE_ENV=production npm run build

# Serve
>>>>>>> Stashed changes
FROM nginx:1.27-alpine

# SPA config
COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
