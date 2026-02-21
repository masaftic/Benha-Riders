import { definePreset } from "@primeuix/themes";

import Aura from '@primeuix/themes/aura';
import Material from '@primeuix/themes/material';
import Lara from '@primeuix/themes/lara';
import Nora from '@primeuix/themes/nora';

export const CustomPreset = definePreset(Nora, {
  semantic: {
    primary: {
      50: '{orange.50}',
      100: '{orange.100}',
      200: '{orange.200}',
      300: '{orange.300}',
      400: '{orange.400}',
      500: '{orange.500}',
      600: '{orange.600}',
      700: '{orange.700}',
      800: '{orange.800}',
      900: '{orange.900}',
      950: '{orange.950}'
    },
    colorScheme: {
      light: {
        primary: {
          color: '{orange.600}',
          inverseColor: '#ffffff',
          hoverColor: '{orange.700}',
          activeColor: '{orange.800}'
        },
        highlight: {
          background: '{orange.100}',
          focusBackground: '{orange.200}',
          color: '{orange.950}',
          focusColor: '{orange.950}'
        }
      }
    }
  }
})
