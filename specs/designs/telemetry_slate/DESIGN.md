---
name: Telemetry Slate
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#45464d'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#76777d'
  outline-variant: '#c6c6cd'
  surface-tint: '#565e74'
  primary: '#000000'
  on-primary: '#ffffff'
  primary-container: '#131b2e'
  on-primary-container: '#7c839b'
  inverse-primary: '#bec6e0'
  secondary: '#006398'
  on-secondary: '#ffffff'
  secondary-container: '#5bb8fe'
  on-secondary-container: '#00476e'
  tertiary: '#000000'
  on-tertiary: '#ffffff'
  tertiary-container: '#001e2c'
  on-tertiary-container: '#008ebf'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dae2fd'
  primary-fixed-dim: '#bec6e0'
  on-primary-fixed: '#131b2e'
  on-primary-fixed-variant: '#3f465c'
  secondary-fixed: '#cce5ff'
  secondary-fixed-dim: '#93ccff'
  on-secondary-fixed: '#001d31'
  on-secondary-fixed-variant: '#004b73'
  tertiary-fixed: '#c4e7ff'
  tertiary-fixed-dim: '#7bd0ff'
  on-tertiary-fixed: '#001e2c'
  on-tertiary-fixed-variant: '#004c69'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
typography:
  headline-xl:
    fontFamily: Geist
    fontSize: 36px
    fontWeight: '600'
    lineHeight: 44px
    letterSpacing: -0.025em
  headline-xl-mobile:
    fontFamily: Geist
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Geist
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Geist
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 30px
    letterSpacing: -0.015em
  headline-md:
    fontFamily: Geist
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.015em
  headline-sm:
    fontFamily: Geist
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.01em
  body-lg:
    fontFamily: Geist
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Geist
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Geist
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  code-lg:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  code-md:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  code-sm:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: '400'
    lineHeight: 14px
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.04em
  label-sm:
    fontFamily: JetBrains Mono
    fontSize: 10px
    fontWeight: '500'
    lineHeight: 12px
    letterSpacing: 0.06em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-desktop: 1.5rem
  margin: 1rem
  margin-desktop: 2rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1rem
  space-xl: 1.5rem
---

## Brand & Style

This design system serves technical operators, systems architects, and backend engineers monitoring multi-region weather telemetry, municipal microservices, and database sync pipelines. The UI prioritizes density, immediate diagnostic clarity, and zero-distraction utilitarianism.

The design movement combines **High-Precision Technical Minimalism** with **Subtle Tactile Engineering**. Interfaces resemble high-performance IDE consoles and telemetry cockpits: ultra-crisp borders, high contrast data blocks, disciplined negative space, and unambiguous semantic color coding. The emotional response is absolute predictability, operational competence, and rapid cognitive ingestion during critical incidents.

## Colors

The palette establishes an authoritative engineering canvas anchored by slate foundations and energized by diagnostic sky-blue spectrums.

- **Primary (`#0f172a`)**: Deep slate; commands high visual gravity, used for high-contrast headers, active control anchors, and structural command rails.
- **Secondary (`#0284c7`)**: High-saturation azure; powers primary interactive triggers, active tabs, selected table rows, and network payload paths.
- **Tertiary (`#38bdf8`)**: Atmospheric sky blue; functions as an illuminated accent for focus rings, secondary badges, graph crosshairs, and live telemetry pulses.
- **Neutral (`#64748b`)**: Balanced slate gray; governs structural strokes, muted metadata, deactivated states, and non-critical axis markers.
- **Canvas & Surface Tiering**:
  - `surface-canvas`: `#f8fafc` (off-white technical bed)
  - `surface-card`: `#ffffff` (pure white data container)
  - `surface-subtle`: `#f1f5f9` (code block backgrounds, input wells, inactive chip fills)
  - `border-subtle`: `#e2e8f0` (1px structure lines)
  - `border-strong`: `#cbd5e1` (divider rules, focused borders)
- **Diagnostic Semantics**:
  - `status-emerald` (`#059669` fill, `#ecfdf5` surface): EF Core connections healthy, telemetry streams active, HTTP 200/201.
  - `status-amber` (`#d97706` fill, `#fffbeb` surface): L2 cache stale, throttling queued, latency threshold warning.
  - `status-rose` (`#e11d48` fill, `#fff1f2` surface): Downstream sensor packet drop, 5xx gateway faults, failover alerts.

## Typography

The typographic hierarchy enforces a dual-engine architecture:

1. **Geist**: Applied across all editorial hierarchy, system dashboards, modal dialogs, and analytical body text. Geist delivers neutral clarity and dense horizontal packing without sacrificing legibility at small sizes.
2. **JetBrains Mono**: Strictly utilized for REST endpoints, CLI parameters, SQL queries, database migration logs, JSON payloads, and technical status chips. All numerical data columns in weather grids (e.g., millibars, latency in ms, wind velocities) must render in tabular JetBrains Mono to preserve vertical digit alignment.

Never introduce serif or decorative display faces. Maintain strict vertical rhythm by pinning all text components to fixed line heights matching the baseline grid.

## Layout & Spacing

This design system uses a strict fluid grid system built on an 8px spatial baseline with 4px micro-subdivisions (`space-xs` = 4px, `space-sm` = 8px, `space-md` = 12px, `space-lg` = 16px, `space-xl` = 24px).

- **Grid Architecture**:
  - **Desktop (≥ 1280px)**: 12-column grid, fluid width with an optional max-width container of `1600px`. Gutters set to `1.5rem` (`24px`), outer canvas margins set to `2rem` (`32px`). Sidebar navigation occupies a fixed 260px rail; API documentation panels dock to a right-hand 380px inspector column.
  - **Tablet (768px – 1279px)**: 8-column layout with `1rem` (`16px`) gutters and `1.5rem` (`24px`) margins. Sidebars collapse to icon-only rails or auto-hiding drawers.
  - **Mobile (< 768px)**: 4-column single-stack layout. Gutters and outer margins set to `1rem` (`16px`). Technical tables transition into swipeable card records or horizontally scrolling monospaced data matrices.
- **Reflow & Alignment**: Layouts must avoid superfluous vertical whitespace. Information density is treated as a feature; dashboards must surface high diagnostic volume above the fold using compact gutters and tight component gaps.

## Elevation & Depth

Visual hierarchy is maintained through **Low-Contrast Structural Outlines** paired with **Micro-Tonal Surface Layering**, eliminating heavy blurry drop shadows in favor of crisp engineering boundaries:

- **Border-Driven Separation**: Every visual module sits on a `#ffffff` card defined by a mandatory `1px solid #e2e8f0` stroke. No component relies on elevation shadows alone to denote boundaries.
- **Elevation Tiers**:
  - **Level 0 (Canvas)**: Background base `#f8fafc`. Flush, non-interactive.
  - **Level 1 (Card & Module)**: `#ffffff`, bordered with `#e2e8f0`. Flat baseline with an ambient structural drop: `box-shadow: 0 1px 2px 0 rgba(15, 23, 42, 0.05)`.
  - **Level 2 (Hover & Active Elements)**: `#ffffff`, stroke elevates to `#cbd5e1`, with subtle focused depth: `box-shadow: 0 4px 6px -1px rgba(15, 23, 42, 0.07), 0 2px 4px -2px rgba(15, 23, 42, 0.04)`.
  - **Level 3 (Overlays, Dropdowns, Slide-overs)**: `#ffffff`, stroke `#cbd5e1`, pronounced ambient depth: `box-shadow: 0 10px 15px -3px rgba(15, 23, 42, 0.08), 0 4px 6px -4px rgba(15, 23, 42, 0.03)`.
- **Code Wells & Telemetry Logs**: Inset depth. Background `#0f172a` (dark terminal theme) with an internal border `1px solid #1e293b`.

## Shapes

The design system enforces a calibrated, sharp corner profile (`roundedness: 1`):

- Standard interactive components (buttons, input fields, badges, code blocks) adopt a strict `0.25rem` (4px) corner radius.
- Structural panels, cards, and modal sheets scale to `rounded-lg` at `0.5rem` (8px).
- Pill shapes are strictly forbidden except for live status pulse pips (4px circles).
- The geometry signals technical rigor, deterministic engineering, and precise tool craftsmanship.

## Components

### Buttons
- **Primary**: Solid `#0f172a` fill, white Geist text (`font-weight: 500`, `14px`), 4px radius, 0-shadow, border: `1px solid transparent`. Hover: `#1e293b`. Focus-visible: 2px offset ring in `#38bdf8`.
- **Secondary (Action)**: Solid `#0284c7` fill, white text. Hover: `#0369a1`.
- **Outline / Ghost**: Background `#ffffff`, border `1px solid #e2e8f0`, text `#0f172a`. Hover: background `#f1f5f9`, border `#cbd5e1`.
- **Utility / Code Action**: JetBrains Mono 12px, height 28px, background `#f1f5f9`, border `1px solid #e2e8f0`, for copying cURL commands or switching JSON/YAML views.

### Status Chips & Badges
- High-visibility diagnostic pills: 4px radius, JetBrains Mono 11px uppercase with `0.05em` letter spacing.
- **Healthy**: `#ecfdf5` background, `#047857` text, 1px border `#a7f3d0`. Includes a 6px solid `#10b981` leading circular dot.
- **Warning / Stale Cache**: `#fffbeb` background, `#b45309` text, 1px border `#fde68a`.
- **Error / Fault**: `#fff1f2` background, `#be123c` text, 1px border `#fecdd3`.
- **HTTP Method Badges**: Fixed-width badges (e.g., `GET` in `#0284c7` tint, `POST` in `#059669` tint, `DELETE` in `#e11d48` tint) rendered in JetBrains Mono 10px bold.

### Input Fields & Selects
- Height 36px (compact desktop default).
- Background `#ffffff`, border `1px solid #cbd5e1`, 4px radius, text Geist 14px `#0f172a`, placeholder `#94a3b8`.
- Focus: Border shifts to `#0284c7` with `box-shadow: 0 0 0 1px #0284c7, 0 0 0 4px rgba(56, 189, 248, 0.2)`.

### Cards & Telemetry Containers
- Background `#ffffff`, 1px border `#e2e8f0`, 8px radius.
- Header bars must include an integrated bottom border (`1px solid #f1f5f9`) separating control metadata from the primary data visualization canvas.
- No internal padding exceeds `space-lg` (16px) to maximize dashboard data density.

### Tables & Data Grids
- Monospaced numerical values, alternating row states (odd rows `#ffffff`, even rows `#f8fafc`).
- Table headers: JetBrains Mono 11px, text `#64748b`, uppercase, border-bottom `1px solid #cbd5e1`. Cell borders `1px solid #f1f5f9`.
- Row height: Compact 36px for high-density scanning; hover state illuminates the entire row with `#f0f9ff` (azure tint).

### Code Blocks & Architecture Specs
- Background `#0f172a`, foreground `#f8fafc`, font JetBrains Mono 13px, line height 1.6.
- Integrated header tab bar indicating endpoint method, route path, runtime execution latency (`24ms`), and a copy payload action. Syntax highlights keyed to `#38bdf8` (keys) and `#34d399` (values).