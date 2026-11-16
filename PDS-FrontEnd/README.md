# PDS web application

Angular 21 application for designing, reviewing, approving, versioning and publishing organizational structures, positions and locations, and for institutional planning: strategic plan, annual plans, budget and requirements. Users sign in through GOB-ID.

See the [repository README](../README.md) for the overall architecture.

## Stack

- Angular 21 and Tailwind CSS 4, with [`@public-intelligence/ui-core`](https://github.com/public-intelligence/UICORE)
- Leaflet for maps, d3-hierarchy for organizational charts, and jsPDF and pdfmake for PDF exports
- Vitest for tests

## Run

`@public-intelligence/ui-core` is installed from GitHub Packages. Add the registry to your user `~/.npmrc`, with a GitHub token that has `read:packages`:

```
@public-intelligence:registry=https://npm.pkg.github.com
//npm.pkg.github.com/:_authToken=${NPM_TOKEN}
```

```bash
npm install
npm start          # http://localhost:4200
npm test           # Vitest
npm run lint
npm run build      # production build in dist/
```

## Configuration

`src/environments/environment.ts` (production) and `environment.development.ts` define:

- `apiUrl`: base URL of the PDS API;
- `gobIdPortalOrigin`, `gobIdApplicationId` and `redirectUri`: the GOB-ID sign-in page, the PDS application id in GOB-ID and the URL GOB-ID returns to;
- `mapTileUrl` and `mapAttribution`: the map tiles;
- `features`, `calendarioLaboral` and `funcionesLocalidad`: feature switches and options of the locations module.
