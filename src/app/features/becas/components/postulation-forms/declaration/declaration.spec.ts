import { Declaration } from './declaration';

describe('Declaration', () => {
  it('opens and closes the expense drawer', () => {
    const component = new Declaration();

    component.openDrawer();
    expect(component.drawer()).toBe(true);

    component.closeDrawer();
    expect(component.drawer()).toBe(false);
  });
});
