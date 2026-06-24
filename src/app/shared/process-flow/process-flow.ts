import { computed, type Signal, signal } from '@angular/core';

export type ProcessStepStatus = 'completed' | 'current' | 'pending';

export interface ProcessStepDefinition<TStep extends string> {
  id: TStep;
  overline: string;
  title: string;
}

export interface ProcessStepItem<TStep extends string> extends ProcessStepDefinition<TStep> {
  status: ProcessStepStatus;
}

export interface ProcessFlow<TStep extends string> {
  readonly currentStep: Signal<TStep>;
  readonly currentIndex: Signal<number>;
  readonly stepItems: Signal<readonly ProcessStepItem<TStep>[]>;
  readonly canGoBack: Signal<boolean>;
  readonly canGoNext: Signal<boolean>;
  next(): boolean;
  previous(): boolean;
  goTo(step: TStep): boolean;
  reset(): void;
}

export function createProcessFlow<TStep extends string>(
  definitions: readonly ProcessStepDefinition<TStep>[],
  initialStep: TStep
): ProcessFlow<TStep> {
  if (definitions.length === 0) {
    throw new Error('Un proceso debe declarar al menos un paso.');
  }

  const uniqueIds = new Set(definitions.map(step => step.id));
  if (uniqueIds.size !== definitions.length) {
    throw new Error('Los identificadores de pasos no pueden repetirse.');
  }

  const initialIndex = definitions.findIndex(step => step.id === initialStep);
  if (initialIndex < 0) {
    throw new Error(`El paso inicial "${initialStep}" no pertenece al proceso.`);
  }

  const currentIndexState = signal(initialIndex);
  const currentIndex = currentIndexState.asReadonly();
  const currentStep = computed(() => definitions[currentIndexState()].id);
  const canGoBack = computed(() => currentIndexState() > 0);
  const canGoNext = computed(() => currentIndexState() < definitions.length - 1);
  const stepItems = computed<readonly ProcessStepItem<TStep>[]>(() => {
    const activeIndex = currentIndexState();
    return definitions.map((step, index) => ({
      ...step,
      status: index < activeIndex ? 'completed' : index === activeIndex ? 'current' : 'pending',
    }));
  });

  function moveTo(index: number): boolean {
    if (index < 0 || index >= definitions.length || index === currentIndexState()) return false;
    currentIndexState.set(index);
    return true;
  }

  return {
    currentStep,
    currentIndex,
    stepItems,
    canGoBack,
    canGoNext,
    next: () => moveTo(currentIndexState() + 1),
    previous: () => moveTo(currentIndexState() - 1),
    goTo: step => moveTo(definitions.findIndex(definition => definition.id === step)),
    reset: () => currentIndexState.set(initialIndex),
  };
}
