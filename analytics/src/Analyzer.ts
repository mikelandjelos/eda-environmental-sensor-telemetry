import {
  NUMERIC_MEASUREMENTS,
  BINARY_MEASUREMENTS,
  EnvironmentalSensorTelemetryDTO,
} from "./EnvironmentalSensorTelemetryDTO";
import { RunningAverageCalculator } from "./RunningAverageCalculator";
import { SensorEvent, SensorEventType } from "./SensorEvent";

type MACAddress = string;
type MeasurementName = string;

export class Analyzator {
  private devices: Map<
    MACAddress,
    Map<MeasurementName, RunningAverageCalculator>
  > = new Map();

  constructor(
    private windowSize: number,
    private deviationPercentAlert: number
  ) {}

  analyzeAndUpdate(
    sensorData: EnvironmentalSensorTelemetryDTO
  ): Array<SensorEvent> {
    const deviceAddress: MACAddress = sensorData.device;

    let measurements:
      | Map<MeasurementName, RunningAverageCalculator>
      | undefined = this.devices.get(deviceAddress);

    // If data from certain device is detected for the first time
    // - setup average calculators.
    if (measurements === undefined) {
      measurements = new Map();

      NUMERIC_MEASUREMENTS.forEach((name: MeasurementName) =>
        measurements?.set(name, new RunningAverageCalculator(this.windowSize))
      );

      this.devices.set(deviceAddress, measurements);
    }

    // Analyzing averageable data
    const sensorEvents: Array<SensorEvent> = [];

    NUMERIC_MEASUREMENTS.forEach((name: MeasurementName) => {
      const measurement: number = sensorData[name];
      const { runningAverage, delta, deltaPercentile } = measurements
        .get(name)!
        .addDataPoint(measurement);

      if (Math.abs(deltaPercentile) >= this.deviationPercentAlert) {
        sensorEvents.push({
          timestamp: sensorData._time,
          type: SensorEventType.NUMERIC,
          device: deviceAddress,
          measurement: name,
          message: `Deviation of ${delta} has been detected for measurement ${name}.`,
          statisticData: {
            currentValue: measurement,
            deviationNominal: delta,
            deviationPercent: deltaPercentile,
            historyLength: this.devices.size,
            runningAverage: runningAverage,
          },
        });
      }
    });

    BINARY_MEASUREMENTS.forEach((name: MeasurementName) => {
      const measurement: boolean = sensorData[name];

      if (measurement === true)
        sensorEvents.push({
          timestamp: sensorData._time,
          type: SensorEventType.BINARY,
          device: deviceAddress,
          measurement: name,
          message: `${name} detected.`,
        });
    });

    return sensorEvents;
  }
}
