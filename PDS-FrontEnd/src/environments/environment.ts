export const environment = {
  production: true,
  apiUrl: 'https://wa-dev-vuc-pdsapi-hxbzcueufkf0hjaw.eastus2-01.azurewebsites.net/api/v1/',
  gobIdPortalOrigin: 'https://mango-bush-0c0d6a50f.1.azurestaticapps.net',
  gobIdApplicationId: 'd2b893c4-9d66-f111-b7fb-6045bdb142ad',
  redirectUri: 'https://proud-smoke-0304ebc0f.2.azurestaticapps.net',
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
