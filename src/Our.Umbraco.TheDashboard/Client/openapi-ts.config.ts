import { defineConfig,defaultPlugins } from '@hey-api/openapi-ts';

export default defineConfig({
	input: 'http://localhost:24760/umbraco/swagger/the-dashboard/swagger.json',
	output: {
		path: './src/backend-api',
	},
	plugins: [
    ...defaultPlugins,
		{
			name: '@hey-api/typescript',
			enums: 'typescript'
		},
		{
			name: '@hey-api/sdk',
			asClass: true,
      classNameBuilder : '{{name}}Resource'
		}
	]
});
