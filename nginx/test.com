server {
    listen 80;
    server_name  ccweb.localtunnel.me;
    location / {
        proxy_pass_header Server;
        proxy_set_header Host $http_host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Scheme $scheme;
        proxy_set_header   X-Forwarded-For  $proxy_add_x_forwarded_for;
        proxy_pass http://127.0.0.1:10080/;
    }
}
