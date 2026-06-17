export interface GoogleMappingConfiguration {
  apiKey: string;
  mapId: string;
  region: string;
  language: string;
  libraries: string[];
  defaultCenter: GoogleMapCenterConfiguration;
  defaultZoom: number;
}

export interface GoogleMapCenterConfiguration {
  latitude: number;
  longitude: number;
}
