FROM node:20-alpine AS build
WORKDIR /src
ARG VITE_MODELFORGE_API_URL=http://localhost:5095
ARG VITE_MODELFORGE_SIDECAR_URL=http://localhost:5200
ARG VITE_MODELFORGE_SIDECAR_TOKEN=
ENV VITE_MODELFORGE_API_URL=$VITE_MODELFORGE_API_URL
ENV VITE_MODELFORGE_SIDECAR_URL=$VITE_MODELFORGE_SIDECAR_URL
ENV VITE_MODELFORGE_SIDECAR_TOKEN=$VITE_MODELFORGE_SIDECAR_TOKEN
COPY src/web/package.json src/web/package-lock.json* ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

FROM nginx:alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY manifest/modelForge.web.xml /usr/share/nginx/html/manifest.xml
EXPOSE 80
