export interface EventModel {
  bitEventId: number;
  bitClientId: number;
  bitResourceId: number;
  startDateTime: string;
  endDateTime: string;
  startAddress?: string | null;
  startLatitude?: number | null;
  startLongitude?: number | null;
  startHexGridId?: number | null;
  endAddress?: string | null;
  endLatitude?: number | null;
  endLongitude?: number | null;
  endHexGridId?: number | null;
  requiresTransportation: boolean;
  requiresReturnTransportation: boolean;
  eventType?: string | null;
  scheduleBitsReserved: boolean;
  createdBy: string;
  createdDate: string;
  updatedBy: string;
  updatedDate?: string | null;
}

export interface EventRequest {
  bitResourceId: number;
  startDateTime: string;
  endDateTime: string;
  startAddress?: string | null;
  startLatitude?: number | null;
  startLongitude?: number | null;
  startHexGridId?: number | null;
  endAddress?: string | null;
  endLatitude?: number | null;
  endLongitude?: number | null;
  endHexGridId?: number | null;
  requiresTransportation: boolean;
  requiresReturnTransportation: boolean;
  eventType?: string | null;
  reserveScheduleBits?: boolean;
  updatedBy?: string;
}

export interface EventListRequest {
  bitResourceIds?: number[] | null;
  rangeStart?: string | null;
  rangeEnd?: string | null;
  eventTypes?: string[] | null;
}
