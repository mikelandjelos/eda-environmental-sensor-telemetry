#!/bin/bash
set -e

clickhouse client -n <<-EOSQL
    CREATE DATABASE environmental_sensor_telemetry;
EOSQL

clickhouse client -n <<-EOSQL
 CREATE TABLE environmental_sensor_telemetry.sensor_data
    (
        timestamp DateTime64(3),
        type String,
        device String,
        measurement String,
        message String,
        current_value Float64,
        deviation_nominal Float64,
        deviation_percent Float64,
        running_average Float64,
        history_length UInt32
    )
    ENGINE = MergeTree()
    ORDER BY (timestamp, device, measurement);
EOSQL