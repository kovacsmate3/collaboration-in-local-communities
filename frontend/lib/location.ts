export interface LocationValue {
  locationText: string
  latitude?: number
  longitude?: number
}

export interface LocationSuggestion extends Required<LocationValue> {
  id: string
}
