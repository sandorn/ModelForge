FROM node:20-alpine AS build
WORKDIR /src
COPY src/web/package.json src/web/package-lock.json* ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

FROM nginx:alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY manifest/modelForge.web.xml /usr/share/nginx/html/manifest.xml
EXPOSE 80
