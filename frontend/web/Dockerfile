FROM node:20 AS build
WORKDIR /app
COPY package.json package-lock.json* pnpm-lock.yaml* yarn.lock* ./
RUN if [ -f package-lock.json ]; then npm ci;     elif [ -f pnpm-lock.yaml ]; then npm i -g pnpm && pnpm i;     elif [ -f yarn.lock ]; then yarn;     else npm i; fi
COPY . .
RUN npm run build
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx","-g","daemon off;"]
