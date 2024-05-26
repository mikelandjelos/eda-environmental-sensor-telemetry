export interface SensorEvent {
  timestamp: string;
  type: string;
  device: string;
  measurement: string;
  message: string;
  statisticData?: {
    // Used for numeric measurements
    currentValue: number;
    deviationNominal: number;
    deviationPercent: number;
    runningAverage: number;
    historyLength: number;
  };
}

export enum SensorEventType {
  NUMERIC = "NUMERIC",
  BINARY = "BINARY",
}
