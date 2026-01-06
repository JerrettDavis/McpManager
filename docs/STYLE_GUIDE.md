# MCP Manager Style Guide

## Overview

MCP Manager uses a modern, comprehensive design system with a soft vaporwave-inspired pastel aesthetic and Material Design-inspired elevation. The color palette features soothing purples, teals, pinks, and blues reminiscent of Material Design's pastel tones. Multi-layered shadows create depth and visual hierarchy throughout the interface. The system supports automatic light/dark themes based on system preferences and provides a consistent, calming visual experience across the application.

## Design Principles

1. **Modern & Clean**: Sleek, minimalist design with ample spacing and soft colors
2. **Consistent**: Unified design language across all components
3. **Accessible**: Focus states, color contrast, and semantic HTML
4. **Responsive**: Mobile-first approach with breakpoints
5. **Theme-Aware**: Automatic light/dark mode support with vaporwave aesthetics
6. **Performance**: CSS custom properties for fast theme switching
7. **Soothing**: Pastel colors that are easy on the eyes and reduce visual fatigue
8. **Material Depth**: Multi-layered shadows create elevation and visual hierarchy

## Color System

### CSS Custom Properties

The color system uses CSS custom properties (CSS variables) for easy theming and consistency.

#### Brand Colors
- `--primary-color: #9c88ff` - Primary brand color (Soft Purple)
- `--primary-dark: #7b68ee` - Darker primary shade
- `--primary-light: #b8a9ff` - Lighter primary shade
- `--secondary-color: #4dd0e1` - Success/secondary color (Pastel Teal)
- `--danger-color: #ff6b9d` - Danger/error color (Soft Pink)
- `--warning-color: #ffb74d` - Warning color (Soft Amber)
- `--info-color: #64b5f6` - Info color (Light Blue)

#### Light Theme
- `--bg-primary: #fafbff` - Primary background (Very light purple tint)
- `--bg-secondary: #f5f7ff` - Secondary background (Soft purple tint)
- `--bg-tertiary: #eef1ff` - Tertiary background (Light purple tint)
- `--bg-elevated: #ffffff` - Elevated surfaces (cards)
- `--text-primary: #1a1a2e` - Primary text
- `--text-secondary: #4a5568` - Secondary text
- `--text-tertiary: #718096` - Tertiary/muted text
- `--border-color: #e8eaf6` - Border color (Soft lavender)

#### Dark Theme
Automatically applied via `@media (prefers-color-scheme: dark)`
- `--bg-primary: #1a1a2e` - Dark primary background (Deep purple-blue)
- `--bg-secondary: #16213e` - Dark secondary background (Navy-purple)
- `--bg-tertiary: #0f3460` - Tertiary background (Deep teal-blue)
- `--text-primary: #e8eaf6` - Light text on dark (Soft lavender)
- `--text-secondary: #b8b8d4` - Secondary text (Muted lavender)
- And more...

## Typography

### Font Family
- System fonts: `-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif`
- Monospace: `'SF Mono', Monaco, 'Cascadia Code', 'Roboto Mono', Consolas, 'Courier New', monospace`

### Headings
- `h1`: 2.25rem (36px), font-weight: 700
- `.display-4`: 2.5rem (40px), font-weight: 700
- `.display-6`: 1.875rem (30px), font-weight: 600
- `h5`: 1.125rem (18px), font-weight: 600
- `h6`: 1rem (16px), font-weight: 600

### Body Text
- Base font size: 16px
- Line height: 1.6
- `.lead`: 1.125rem, font-weight: 400
- `.small`: 0.875rem

## Components

### Buttons

#### Primary Button
```html
<button class="btn btn-primary">Primary Button</button>
```
- Background: `--primary-color`
- Hover: Darker shade with elevation
- Transform: translateY(-1px) on hover

#### Secondary Button
```html
<button class="btn btn-secondary">Secondary Button</button>
```

#### Outline Buttons
```html
<button class="btn btn-outline-primary">Outline Primary</button>
<button class="btn btn-outline-secondary">Outline Secondary</button>
<button class="btn btn-outline-danger">Outline Danger</button>
```

#### Sizes
- `.btn-sm`: Small button
- `.btn`: Default button
- `.btn-lg`: Large button

#### States
- `:hover` - Elevation and transform
- `:disabled` - Reduced opacity, no interactions
- `:focus-visible` - Focus ring for accessibility

### Cards

```html
<div class="card">
  <div class="card-header">Card Header</div>
  <div class="card-body">
    <h5 class="card-title">Card Title</h5>
    <h6 class="card-subtitle">Card Subtitle</h6>
    <p class="card-text">Card content goes here.</p>
  </div>
  <div class="card-footer">Card Footer</div>
</div>
```

#### Features
- Border radius: 12px
- Box shadow on hover
- Smooth transitions
- Focus-within effect
- Colored headers: `.bg-primary`, `.bg-success`, `.bg-info`, etc.

### Forms

```html
<input type="text" class="form-control" placeholder="Enter text...">
<select class="form-select">
  <option>Choose an option</option>
</select>
<textarea class="form-control"></textarea>
```

#### Features
- Clean borders with rounded corners (8px)
- Focus state with primary color ring
- Consistent padding and sizing

### Tabs

```html
<ul class="nav nav-tabs">
  <li class="nav-item">
    <button class="nav-link active">Tab 1</button>
  </li>
  <li class="nav-item">
    <button class="nav-link">Tab 2</button>
  </li>
</ul>
<div class="tab-content">
  <!-- Tab content -->
</div>
```

### List Groups

```html
<div class="list-group">
  <div class="list-group-item">
    <h6>Item Title</h6>
    <small class="text-muted">Item description</small>
  </div>
</div>
```

### Badges

```html
<span class="badge bg-primary">Primary</span>
<span class="badge bg-secondary">Secondary</span>
<span class="badge bg-success">Success</span>
<span class="badge bg-danger">Danger</span>
<span class="badge bg-warning">Warning</span>
<span class="badge bg-info">Info</span>
```

### Alerts

```html
<div class="alert alert-info">
  <h4 class="alert-heading">Info Alert</h4>
  <p>This is an informational message.</p>
</div>

<div class="alert alert-warning">Warning message</div>
<div class="alert alert-danger">Danger message</div>
```

### Pagination

```html
<ul class="pagination">
  <li class="page-item disabled">
    <span class="page-link">Previous</span>
  </li>
  <li class="page-item active">
    <span class="page-link">1</span>
  </li>
  <li class="page-item">
    <a class="page-link" href="#">2</a>
  </li>
  <li class="page-item">
    <a class="page-link" href="#">Next</a>
  </li>
</ul>
```

## Layout

### Container
```html
<div class="container-fluid">
  <!-- Content with max-width: 1400px -->
</div>
```

### Grid System
```html
<div class="row">
  <div class="col-md-4">Column 1</div>
  <div class="col-md-4">Column 2</div>
  <div class="col-md-4">Column 3</div>
</div>
```

#### Breakpoints
- Mobile: < 768px (full width columns)
- Desktop: ≥ 768px (grid columns active)

## Utility Classes

### Spacing
- Margin: `.mb-1` through `.mb-5`, `.mt-2`, `.mt-3`, `.mt-4`, `.me-1`, `.me-2`, `.me-3`, `.ms-2`
- Padding: `.p-2`, `.p-3`, `.p-4`, `.px-3`, `.px-4`, `.py-2`, `.py-3`, `.py-4`, `.py-5`
- Gap: `.gap-1`, `.gap-2`, `.gap-3`, `.gap-4`

### Display
- `.d-flex` - Flexbox
- `.d-grid` - Grid
- `.d-block` - Block
- `.d-none` - Hidden
- `.d-inline-block` - Inline block

### Flexbox
- `.justify-content-between`
- `.justify-content-center`
- `.justify-content-end`
- `.align-items-center`
- `.align-items-start`
- `.flex-wrap`
- `.flex-column`
- `.flex-1`

### Sizing
- `.w-100` - Width 100%
- `.h-100` - Height 100%

### Text
- `.text-center` - Centered text
- `.text-end` - Right-aligned text
- `.text-muted` - Muted text color
- `.text-break` - Word break
- `.text-white` - White text
- `.text-dark` - Dark text
- `.text-primary` - Primary color text
- `.small` - Smaller font size
- `.font-monospace` - Monospace font

## Shadows

The design system includes 5 levels of shadows:
- `--shadow-sm` - Subtle shadow
- `--shadow` - Default shadow
- `--shadow-md` - Medium shadow
- `--shadow-lg` - Large shadow
- `--shadow-xl` - Extra large shadow

## Transitions

Standard transition durations:
- `--transition-fast: 150ms` - Quick interactions
- `--transition: 300ms` - Standard transitions
- `--transition-slow: 500ms` - Slow, emphasized transitions

Easing: `cubic-bezier(0.4, 0, 0.2, 1)` - Standard easing curve

## Material Design Elevation

The design system uses Material Design-inspired elevation with multi-layered shadows to create depth and visual hierarchy.

### Shadow Levels

The system includes 6 levels of elevation:
- `--shadow-sm` - Subtle elevation (1dp) for small elements like badges, form controls
- `--shadow` - Default elevation (2dp) for cards and containers at rest
- `--shadow-md` - Medium elevation (4dp) for buttons on hover, raised cards
- `--shadow-lg` - Large elevation (8dp) for floating action buttons, important dialogs
- `--shadow-xl` - Extra large elevation (16dp) for modals and overlays
- `--shadow-2xl` - Maximum elevation (24dp) for high-priority overlays

### Usage Guidelines

**Cards:**
- Base state: `--shadow` for resting elevation
- Hover state: `--shadow-lg` for interactive feedback
- Focus-within: `--shadow-xl` for active form context

**Buttons:**
- Base state: `--shadow-sm` for subtle depth
- Hover state: `--shadow-md` with translateY(-2px) for elevation
- Active state: Return to `--shadow-sm` for pressed effect

**Forms:**
- Base state: `--shadow-sm` for subtle depth
- Focus state: `--shadow` combined with colored ring for emphasis

**Interactive Lists:**
- Container: `--shadow-sm` for subtle depth
- Hover state: `--shadow` on individual items with z-index layering

### Dark Mode Adjustments

In dark mode, shadows are more pronounced (higher opacity) to maintain depth perception against dark backgrounds. The shadow values automatically adjust via CSS custom properties.

## Animations

### Built-in Animations
- `fadeIn` - Fade in from transparent
- `slideIn` - Slide in from top with fade
- `spinner-border` - Loading spinner rotation

## Accessibility

### Focus Visible
All interactive elements have visible focus states with:
- 2px outline in primary color
- 2px offset from element
- Applied only on keyboard focus (`:focus-visible`)

### Color Contrast
All color combinations meet WCAG AA standards for contrast ratios.

### Semantic HTML
Use appropriate HTML elements for their intended purpose.

## Best Practices

1. **Use CSS Variables**: Always use CSS custom properties for colors, spacing, and theme values
2. **Consistent Spacing**: Use the spacing scale (0.25rem, 0.5rem, 1rem, 1.5rem, 3rem)
3. **Border Radius**: Use consistent values (4px, 6px, 8px, 10px, 12px)
4. **Transitions**: Add transitions to interactive elements for smooth UX
5. **Hover Effects**: Provide visual feedback on hover with transforms and shadows
6. **Focus States**: Always ensure keyboard navigation is visually clear
7. **Responsive**: Test all layouts at mobile and desktop sizes
8. **Dark Mode**: Verify all components in both light and dark themes

## Examples

### Card with Actions
```html
<div class="card">
  <div class="card-header bg-primary text-white">
    Featured Item
  </div>
  <div class="card-body">
    <h5 class="card-title">Card Title</h5>
    <p class="card-text">Description of the card content.</p>
    <div class="d-flex gap-2">
      <button class="btn btn-primary">Primary Action</button>
      <button class="btn btn-outline-secondary">Secondary</button>
    </div>
  </div>
</div>
```

### Form with Validation
```html
<div class="mb-3">
  <input type="text" class="form-control" placeholder="Username">
</div>
<div class="mb-3">
  <input type="password" class="form-control" placeholder="Password">
</div>
<div class="alert alert-danger">
  Invalid credentials
</div>
<button class="btn btn-primary w-100">Sign In</button>
```

### Dashboard Stats
```html
<div class="row">
  <div class="col-md-4">
    <div class="card bg-primary text-white">
      <div class="card-body">
        <h5 class="card-title">Total Users</h5>
        <p class="display-6">1,234</p>
      </div>
    </div>
  </div>
  <div class="col-md-4">
    <div class="card bg-success text-white">
      <div class="card-body">
        <h5 class="card-title">Active Now</h5>
        <p class="display-6">567</p>
      </div>
    </div>
  </div>
  <div class="col-md-4">
    <div class="card bg-info text-white">
      <div class="card-body">
        <h5 class="card-title">Growth</h5>
        <p class="display-6">+23%</p>
      </div>
    </div>
  </div>
</div>
```

## Migration Notes

If updating from an older version:
1. Replace Bootstrap-specific classes with the new utility classes
2. Update color references to use CSS custom properties
3. Add focus-visible states to custom interactive elements
4. Test in both light and dark modes
5. Verify responsive behavior at all breakpoints

## Resources

- Color palette inspired by Tailwind CSS
- Design patterns from shadcn/ui
- Typography scale based on modular scale principles
- Accessibility guidelines from WCAG 2.1
