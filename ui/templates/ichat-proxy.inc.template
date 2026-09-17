# Shared proxy settings for every route that reaches the API.
#
# proxy_buffering off is the load-bearing line: the chat endpoint answers with
# Server-Sent Events, and with buffering on nginx holds the tokens until the
# response completes, so the answer arrives in one lump at the end instead of
# streaming. HTTP/1.1 with an empty Connection header keeps that channel open.

proxy_http_version 1.1;
proxy_set_header Connection '';
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;

proxy_buffering off;
proxy_cache off;

# A slow provider can take a while to produce the first token.
proxy_read_timeout 10m;
proxy_send_timeout 10m;
