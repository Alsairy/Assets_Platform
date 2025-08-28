# Build
FROM node:20-alpine AS build
WORKDIR /app

# Better cache: copy only manifests first, install, then copy src
COPY package*.json ./
ENV NODE_ENV=production
RUN npm ci

COPY . .
RUN npm run build

# Serve
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
