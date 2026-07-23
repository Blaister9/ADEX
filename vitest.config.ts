import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

/**
 * Single workspace-level Vitest configuration. Test tooling is installed once at
 * the repository root so every TypeScript package shares the same versions.
 */
export default defineConfig({
  test: {
    projects: [
      {
        test: {
          name: 'sdk-web',
          root: './packages/sdk-web',
          environment: 'happy-dom',
          include: ['test/**/*.test.ts'],
        },
      },
      {
        test: {
          name: 'contracts',
          root: './packages/contracts',
          environment: 'node',
          include: ['test/**/*.test.ts'],
        },
      },
      {
        plugins: [react()],
        test: {
          name: 'dashboard',
          root: './apps/dashboard',
          environment: 'happy-dom',
          include: ['test/**/*.test.tsx'],
        },
      },
    ],
  },
});
