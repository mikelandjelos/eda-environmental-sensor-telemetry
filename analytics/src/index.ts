import mqtt from "mqtt";
import { EnvironmentalSensorTelemetryDTO } from "./EnvironmentalSensorTelemetryDTO";
import { Analyzator as Analyzer } from "./Analyzer";
import { SensorEvent } from "./SensorEvent";

function main() {
  const {
    MQTT_BROKER_HOST,
    MQTT_BROKER_PORT,
    TARGET_DEVICE,
    SAMPLE_WINDOW_SIZE,
    DEVIATION_PERCENT_THRESHOLD,
  } = process.env;

  if (MQTT_BROKER_HOST == undefined)
    throw `Missing environment variable for MQTT_BROKER_HOST`;

  if (MQTT_BROKER_PORT == undefined)
    throw `Missing environment variable for MQTT_BROKER_PORT`;

  if (SAMPLE_WINDOW_SIZE == undefined)
    throw `Missing environment variable for SAMPLE_WINDOW_SIZE`;

  if (DEVIATION_PERCENT_THRESHOLD == undefined)
    throw `Missing environment variable for DEVIATION_PERCENT_THRESHOLD`;

  const connectUrl = `mqtt://${MQTT_BROKER_HOST}:${MQTT_BROKER_PORT}`;

  const sensorMeasurementsTopic = `sensor/measurements/${TARGET_DEVICE ?? "#"}`; // `#` means ALL DEVICES - wildcard

  const clientId = Math.floor(Math.random() * 0xffffff)
    .toString(16)
    .padStart(8, "0");

  console.log(`Analytics service number ${clientId} is starting!`);

  const client = mqtt.connect(connectUrl, {
    clientId: `est-analytics-${clientId}`,
    connectTimeout: 4000,
    reconnectPeriod: 1000,
  });

  client.on("connect", () => {
    console.log("Connected");
    client.subscribe(sensorMeasurementsTopic, () => {
      console.log(`Subscribed to topic '${sensorMeasurementsTopic}'`);
    });
  });

  const analyzator: Analyzer = new Analyzer(
    Number.parseInt(SAMPLE_WINDOW_SIZE),
    Number.parseInt(DEVIATION_PERCENT_THRESHOLD)
  );

  client.on("message", (topic, payload) => {
    const environmentalSensorData: EnvironmentalSensorTelemetryDTO = JSON.parse(
      payload.toString()
    );

    const sensorEvents = analyzator.analyzeAndUpdate(environmentalSensorData);

    const sensorEventsTopic = `sensor/events/${environmentalSensorData.device}`;

    sensorEvents.forEach((event: SensorEvent) => {
      client.publish(
        sensorEventsTopic,
        JSON.stringify(event),
        {
          qos: 1,
        },
        (err) => {
          if (err) {
            console.error(`Failed to publish message: ${err}`);
          } else {
            console.log(`Event published successfully: ${event}`);
          }
        }
      );
    });
  });

  client.on("error", (err) => {
    console.error("Error:", err);
  });
}

if (require.main === module) {
  main();
}
