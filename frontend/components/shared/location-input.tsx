"use client"

import { Location01Icon, Search01Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"
import * as React from "react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import type { LocationSuggestion, LocationValue } from "@/lib/location"

interface LocationInputProps {
  id: string
  label: string
  value: LocationValue
  onChange: (value: LocationValue) => void
  required?: boolean
  placeholder?: string
}

interface SearchResponse {
  suggestions: LocationSuggestion[]
}

interface ReverseResponse {
  location: LocationSuggestion
}

export function LocationInput({
  id,
  label,
  value,
  onChange,
  required = false,
  placeholder = "City, neighbourhood, or street address",
}: LocationInputProps) {
  const [suggestions, setSuggestions] = React.useState<LocationSuggestion[]>([])
  const [isSearching, setIsSearching] = React.useState(false)
  const [isLocating, setIsLocating] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)

  function updateText(locationText: string) {
    onChange({ locationText })
    setSuggestions([])
    setError(null)
  }

  async function lookupAddress() {
    const query = value.locationText.trim()
    if (query.length < 3) {
      setError("Enter at least 3 characters.")
      setSuggestions([])
      return
    }

    setIsSearching(true)
    setError(null)

    try {
      const response = await fetch(
        `/api/locations/search?q=${encodeURIComponent(query)}`,
        { cache: "no-store" }
      )

      if (!response.ok) {
        setError("Address lookup is unavailable.")
        setSuggestions([])
        return
      }

      const data = (await response.json()) as SearchResponse
      setSuggestions(data.suggestions)
      if (data.suggestions.length === 0) {
        setError("No matching addresses found.")
      }
    } catch {
      setError("Address lookup is unavailable.")
      setSuggestions([])
    } finally {
      setIsSearching(false)
    }
  }

  function locateUser() {
    if (!navigator.geolocation) {
      setError("Your browser does not support location lookup.")
      return
    }

    setIsLocating(true)
    setError(null)

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const latitude = position.coords.latitude
        const longitude = position.coords.longitude

        onChange({
          locationText: `${latitude.toFixed(6)}, ${longitude.toFixed(6)}`,
          latitude,
          longitude,
        })
        void reverseGeocode(latitude, longitude)
      },
      () => {
        setError("Unable to read your current location.")
        setIsLocating(false)
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
    )
  }

  async function reverseGeocode(latitude: number, longitude: number) {
    try {
      const params = new URLSearchParams({
        lat: String(latitude),
        lon: String(longitude),
      })
      const response = await fetch(`/api/locations/reverse?${params}`, {
        cache: "no-store",
      })

      if (!response.ok) {
        onChange({
          locationText: `${latitude.toFixed(6)}, ${longitude.toFixed(6)}`,
          latitude,
          longitude,
        })
        setError("Address name unavailable; coordinates were saved.")
        return
      }

      const data = (await response.json()) as ReverseResponse
      onChange(data.location)
      setSuggestions([])
    } catch {
      onChange({
        locationText: `${latitude.toFixed(6)}, ${longitude.toFixed(6)}`,
        latitude,
        longitude,
      })
      setError("Address name unavailable; coordinates were saved.")
    } finally {
      setIsLocating(false)
    }
  }

  function selectSuggestion(suggestion: LocationSuggestion) {
    onChange(suggestion)
    setSuggestions([])
    setError(null)
  }

  return (
    <div className="grid gap-3">
      <Label htmlFor={id}>{label}</Label>
      <div className="grid gap-2">
        <Input
          id={id}
          required={required}
          placeholder={placeholder}
          value={value.locationText}
          onChange={(event) => updateText(event.target.value)}
        />

        <div className="grid gap-2 sm:grid-cols-2">
          <Button
            type="button"
            variant="outline"
            disabled={isSearching}
            onClick={lookupAddress}
          >
            <HugeiconsIcon icon={Search01Icon} className="size-4" />
            {isSearching ? "Looking up..." : "Lookup address"}
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={isLocating}
            onClick={locateUser}
          >
            <HugeiconsIcon icon={Location01Icon} className="size-4" />
            {isLocating ? "Locating..." : "Use my location"}
          </Button>
        </div>

        {suggestions.length > 0 ? (
          <div className="overflow-hidden rounded-md border border-border bg-background">
            {suggestions.map((suggestion) => (
              <button
                key={suggestion.id}
                type="button"
                className="block w-full border-b border-border px-3 py-2 text-left text-sm last:border-b-0 hover:bg-muted"
                onClick={() => selectSuggestion(suggestion)}
              >
                {suggestion.locationText}
              </button>
            ))}
          </div>
        ) : null}

        {error ? <p className="text-xs text-destructive">{error}</p> : null}
        <p className="text-xs text-muted-foreground">
          Address data &copy; OpenStreetMap contributors.
        </p>
      </div>
    </div>
  )
}
