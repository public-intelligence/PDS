export const environment = {
  production: false,
  apiUrl: 'https://localhost:7247/api/v1/',
  gobIdPortalOrigin: 'https://mango-bush-0c0d6a50f.1.azurestaticapps.net',
  gobIdApplicationId: '4f4404f2-2364-f111-b7f9-6045bdb142ad',
  redirectUri: 'https://mango-rock-06763dd0f.7.azurestaticapps.net',
  mapTileUrl: 'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png',
  mapAttribution:
    '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors ' +
    '&copy; <a href="https://carto.com/attributions">CARTO</a>',
  features: {
    // Refleja `Calendario:Habilitado` del backend: ambos se encienden juntos.
    calendarioLaboral: true,
    // Funciones de localidad (LOC-M6): requiere el catálogo `funciones-localidad` del backend.
    funcionesLocalidad: true,
  },
};
