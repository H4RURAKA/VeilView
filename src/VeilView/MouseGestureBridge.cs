namespace VeilView;

internal static class MouseGestureBridge
{
    public const string Script = """
(() => {
  if (window.__veilViewMouseGestureInstalled) return;
  window.__veilViewMouseGestureInstalled = true;

  const minSegmentDistance = 28;
  const startThreshold = 14;
  let tracking = false;
  let moved = false;
  let suppressContextMenu = false;
  let startX = 0;
  let startY = 0;
  let lastX = 0;
  let lastY = 0;
  let dirs = [];

  function pushDirection(dx, dy, x, y) {
    if (Math.hypot(dx, dy) < minSegmentDistance) return;

    const direction = Math.abs(dx) >= Math.abs(dy)
      ? (dx > 0 ? 'R' : 'L')
      : (dy > 0 ? 'D' : 'U');

    if (dirs.length === 0 || dirs[dirs.length - 1] !== direction) {
      dirs.push(direction);
      if (dirs.length > 4) dirs = dirs.slice(dirs.length - 4);
    }

    lastX = x;
    lastY = y;
  }

  function classify(sequence) {
    if (!sequence || sequence.length === 0) return '';

    if (sequence.length === 1) {
      if (sequence[0] === 'L') return 'Left';
      if (sequence[0] === 'R') return 'Right';
      return '';
    }

    const first = sequence[0];
    const second = sequence[1];

    if ((first === 'U' && second === 'D') || (first === 'D' && second === 'U')) return 'Vertical';
    if ((first === 'L' && second === 'R') || (first === 'R' && second === 'L')) return 'Horizontal';

    // The corner shapes are normalized by shape, not by where the user starts drawing.
    if ((first === 'U' && second === 'R') || (first === 'L' && second === 'D')) return 'CornerTopLeft';
    if ((first === 'U' && second === 'L') || (first === 'R' && second === 'D')) return 'CornerTopRight';
    if ((first === 'D' && second === 'L') || (first === 'R' && second === 'U')) return 'CornerBottomRight';
    if ((first === 'D' && second === 'R') || (first === 'L' && second === 'U')) return 'CornerBottomLeft';

    return '';
  }

  function postGesture(pattern) {
    if (!pattern || !window.chrome || !window.chrome.webview) return;

    window.chrome.webview.postMessage({
      type: 'veilviewGesture',
      pattern,
      sequence: dirs.join('')
    });
  }

  document.addEventListener('pointerdown', event => {
    if (event.button !== 2) return;

    tracking = true;
    moved = false;
    suppressContextMenu = false;
    startX = event.clientX;
    startY = event.clientY;
    lastX = event.clientX;
    lastY = event.clientY;
    dirs = [];

    try {
      if (event.target && event.target.setPointerCapture) {
        event.target.setPointerCapture(event.pointerId);
      }
    } catch (_) {
      // Pointer capture is best-effort only.
    }
  }, true);

  document.addEventListener('pointermove', event => {
    if (!tracking) return;

    const total = Math.hypot(event.clientX - startX, event.clientY - startY);
    if (total >= startThreshold) {
      moved = true;
      event.preventDefault();
      event.stopPropagation();
    }

    if (moved) {
      pushDirection(event.clientX - lastX, event.clientY - lastY, event.clientX, event.clientY);
    }
  }, true);

  document.addEventListener('pointerup', event => {
    if (!tracking || event.button !== 2) return;

    if (moved) {
      event.preventDefault();
      event.stopPropagation();
      suppressContextMenu = true;
      postGesture(classify(dirs));
      setTimeout(() => { suppressContextMenu = false; }, 150);
    }

    tracking = false;
    moved = false;
  }, true);

  document.addEventListener('pointercancel', () => {
    tracking = false;
    moved = false;
  }, true);

  window.addEventListener('blur', () => {
    tracking = false;
    moved = false;
  }, true);

  document.addEventListener('contextmenu', event => {
    if (!suppressContextMenu && !moved) return;
    event.preventDefault();
    event.stopPropagation();
  }, true);
})();
""";
}
