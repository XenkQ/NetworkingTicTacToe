/**
 * Logical viewport dimensions and high-DPI rendering multiplier for the Phaser canvas.
 */
export const LOGICAL_WIDTH = 800;
export const LOGICAL_HEIGHT = 600;

const detectedDevicePixelRatio = typeof window !== 'undefined' ? window.devicePixelRatio || 1 : 1;

export const RENDER_RESOLUTION = Math.min(
  Math.max(Math.ceil(detectedDevicePixelRatio), 2),
  3
);
