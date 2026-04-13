// Execution gateway: consume approved intents from Redis, submit to broker (Alpaca), record results.
// Hot path — keep free of LLM calls.
package main

import (
	"context"
	"log"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/redis/go-redis/v9"
)

func main() {
	addr := getenv("REDIS_ADDR", "localhost:6379")
	rdb := redis.NewClient(&redis.Options{Addr: addr})
	ctx := context.Background()

	if err := rdb.Ping(ctx).Err(); err != nil {
		log.Printf("redis ping: %v (continuing — may retry)", err)
	}

	log.Printf("agent-mesh-execution gateway | REDIS_ADDR=%s", addr)
	// TODO: BRPOP approved:intents, decode ApprovedIntent v1, Alpaca submit, XADD results

	sig := make(chan os.Signal, 1)
	signal.Notify(sig, syscall.SIGINT, syscall.SIGTERM)
	<-sig
	log.Println("shutdown")
	_ = rdb.Close()
}

func getenv(k, def string) string {
	if v := os.Getenv(k); v != "" {
		return v
	}
	return def
}

func init() {
	_ = time.UTC
}
