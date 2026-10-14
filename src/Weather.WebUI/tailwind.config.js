// Telemetry Slate tokens, copied verbatim from specs/designs/weather_explorer_asp.net_core_client/code.html.
const design = {
    darkMode: "class",
    theme: {
      extend: {
        "colors": {
          "on-tertiary-fixed": "#001e2c",
          "surface": "#f8f9ff",
          "on-tertiary-fixed-variant": "#004c69",
          "error-container": "#ffdad6",
          "surface-container-highest": "#d3e4fe",
          "inverse-surface": "#213145",
          "error": "#ba1a1a",
          "tertiary-container": "#001e2c",
          "on-tertiary": "#ffffff",
          "inverse-primary": "#bec6e0",
          "on-primary-fixed": "#131b2e",
          "surface-variant": "#d3e4fe",
          "tertiary-fixed-dim": "#7bd0ff",
          "surface-container": "#e5eeff",
          "surface-tint": "#565e74",
          "on-error-container": "#93000a",
          "primary-fixed-dim": "#bec6e0",
          "surface-dim": "#cbdbf5",
          "tertiary": "#000000",
          "on-primary": "#ffffff",
          "on-error": "#ffffff",
          "surface-bright": "#f8f9ff",
          "primary-fixed": "#dae2fd",
          "secondary-fixed": "#cce5ff",
          "on-secondary-fixed-variant": "#004b73",
          "background": "#f8f9ff",
          "on-secondary-container": "#00476e",
          "surface-container-lowest": "#ffffff",
          "outline": "#76777d",
          "on-secondary-fixed": "#001d31",
          "surface-container-high": "#dce9ff",
          "on-primary-container": "#7c839b",
          "primary": "#000000",
          "surface-container-low": "#eff4ff",
          "on-secondary": "#ffffff",
          "inverse-on-surface": "#eaf1ff",
          "on-surface": "#0b1c30",
          "secondary-fixed-dim": "#93ccff",
          "on-primary-fixed-variant": "#3f465c",
          "secondary": "#006398",
          "on-background": "#0b1c30",
          "primary-container": "#131b2e",
          "secondary-container": "#5bb8fe",
          "on-tertiary-container": "#008ebf",
          "on-surface-variant": "#45464d",
          "tertiary-fixed": "#c4e7ff",
          "outline-variant": "#c6c6cd"
        },
        "borderRadius": {
          "DEFAULT": "0.125rem",
          "lg": "0.25rem",
          "xl": "0.5rem",
          "full": "0.75rem"
        },
        "spacing": {
          "space-xs": "0.25rem",
          "gutter": "1rem",
          "space-sm": "0.5rem",
          "gutter-desktop": "1.5rem",
          "margin": "1rem",
          "space-xl": "1.5rem",
          "margin-desktop": "2rem",
          "space-lg": "1rem",
          "space-md": "0.75rem"
        },
        "fontFamily": {
          "code-sm": ["JetBrains Mono"],
          "headline-sm": ["Geist"],
          "label-md": ["JetBrains Mono"],
          "code-md": ["JetBrains Mono"],
          "code-lg": ["JetBrains Mono"],
          "headline-lg": ["Geist"],
          "label-sm": ["JetBrains Mono"],
          "headline-xl-mobile": ["Geist"],
          "headline-lg-mobile": ["Geist"],
          "headline-md": ["Geist"],
          "headline-xl": ["Geist"],
          "body-sm": ["Geist"],
          "body-md": ["Geist"],
          "body-lg": ["Geist"]
        },
        "fontSize": {
          "code-sm": ["11px", {"lineHeight": "14px", "fontWeight": "400"}],
          "headline-sm": ["16px", {"lineHeight": "24px", "letterSpacing": "-0.01em", "fontWeight": "600"}],
          "label-md": ["12px", {"lineHeight": "16px", "letterSpacing": "0.04em", "fontWeight": "500"}],
          "code-md": ["13px", {"lineHeight": "18px", "fontWeight": "400"}],
          "code-lg": ["14px", {"lineHeight": "20px", "fontWeight": "500"}],
          "headline-lg": ["28px", {"lineHeight": "36px", "letterSpacing": "-0.02em", "fontWeight": "600"}],
          "label-sm": ["10px", {"lineHeight": "12px", "letterSpacing": "0.06em", "fontWeight": "500"}],
          "headline-xl-mobile": ["28px", {"lineHeight": "36px", "letterSpacing": "-0.02em", "fontWeight": "600"}],
          "headline-lg-mobile": ["22px", {"lineHeight": "30px", "letterSpacing": "-0.015em", "fontWeight": "600"}],
          "headline-md": ["20px", {"lineHeight": "28px", "letterSpacing": "-0.015em", "fontWeight": "600"}],
          "headline-xl": ["36px", {"lineHeight": "44px", "letterSpacing": "-0.025em", "fontWeight": "600"}],
          "body-sm": ["12px", {"lineHeight": "16px", "fontWeight": "400"}],
          "body-md": ["14px", {"lineHeight": "20px", "fontWeight": "400"}],
          "body-lg": ["16px", {"lineHeight": "24px", "fontWeight": "400"}]
        }
      }
    }
  };

module.exports = {
  content: ["./Components/**/*.razor", "./wwwroot/**/*.html"],
  // icon-fill is the CSP-safe replacement for the prototype's inline font-variation-settings;
  // keep it emitted even before the pages that use it exist.
  safelist: ["icon-fill"],
  ...design,
};
