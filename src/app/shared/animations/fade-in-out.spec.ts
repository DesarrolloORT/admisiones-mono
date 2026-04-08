import { fadeInOut } from './fade-in-out';

type TransitionDefinitionLike = {
  animation?: unknown;
  expr?: string;
};

describe('fadeInOut Animation', () => {
  it('should have the trigger name "fadeInOut"', () => {
    expect(fadeInOut.name).toBe('fadeInOut');
  });

  it('should define two transitions', () => {
    expect(Array.isArray(fadeInOut.definitions)).toBe(true);
    expect(fadeInOut.definitions.length).toBe(2);
  });

  it('should have a ":enter" transition defined with animation steps', () => {
    const definitions = fadeInOut.definitions as TransitionDefinitionLike[];

    // Find the transition that applies to the ":enter" state.
    const enterTransition = definitions.find(def => def.expr === ':enter');
    expect(enterTransition).toBeDefined();

    // Verify that there is an animation sequence provided.
    // Note: The internal structure of the "animation" may be complex,
    // so we simply check that it exists.
    expect(enterTransition?.animation).toBeDefined();

    // Optionally, you can also test that the animation string is present
    // in one of the animation steps. For example, checking that '200ms' exists.
    const animationStr = JSON.stringify(enterTransition?.animation);
    expect(animationStr).toContain('200ms');
    expect(animationStr).toContain('ease-in');
  });

  it('should have a ":leave" transition defined with animation steps', () => {
    const definitions = fadeInOut.definitions as TransitionDefinitionLike[];

    // Find the transition that applies to the ":leave" state.
    const leaveTransition = definitions.find(def => def.expr === ':leave');
    expect(leaveTransition).toBeDefined();

    // Verify that there is an animation sequence provided.
    expect(leaveTransition?.animation).toBeDefined();

    // Optionally, verify that the timing for the leave animation is correct.
    const animationStr = JSON.stringify(leaveTransition?.animation);
    expect(animationStr).toContain('200ms');
    expect(animationStr).toContain('ease-out');
  });
});

