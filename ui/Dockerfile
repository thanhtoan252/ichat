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

COPY templates/ /etc/nginx/templates/
# nginx resolves the upstream name once at startup, so this is the whole knob for
# where /api goes. See ui/templates/default.conf.template.
ENV ICHAT_API_UPSTREAM=api:8080
# Without the filter envsubst would eat $host, $uri and every other nginx variable.
ENV NGINX_ENVSUBST_FILTER=^ICHAT_
COPY --from=build /src/dist/ui/browser /usr/share/nginx/html

EXPOSE 80
