`docker run --rm --name test-redis -p 6379:6379 redis`

`docker exec -it test-redis redis-cli`

`docker stop test-redis`

Remove --rm if you want to persist container and restart later

`docker start test-redis`
