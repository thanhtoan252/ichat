FROM node:24-alpine AS build
WORKDIR /src

# Manifests first: this layer stays cached until a dependency actually changes,
# so editing source does not re-run the install.
COPY package.json package-lock.json ./
# Keeps the CLI from trying to prompt about analytics on a non-interactive build.
ENV NG_CLI_ANALYTICS=false
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.29-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY nginx-proxy.inc /etc/nginx/conf.d/ichat-proxy.inc
COPY --from=build /src/dist/ui/browser /usr/share/nginx/html

EXPOSE 80
