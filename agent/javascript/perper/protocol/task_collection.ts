import CancelablePromise from "cancelable-promise";

export class TaskCollection {
    tasks: Set<CancelablePromise<any>>;
    tasksLeft: number;

    constructor() {
        this.tasks = new Set();
        this.tasksLeft = 0;
    }

    remove(task: CancelablePromise<any>) {
        if (this.tasks.has(task)) {
            this.tasks.delete(task);
            this.tasksLeft -= 1;
        }
    }

    add(task: () => Promise<any>) {
        const cancelablePromise = new CancelablePromise(task);
        this.tasks.add(cancelablePromise);
        this.tasksLeft += 1;
        cancelablePromise
            .then(() => {
                this.remove(cancelablePromise);
            })
            .catch((error) => {
                if (!cancelablePromise.isCanceled) {
                    throw error;
                }
            });
    }

    //TODO: find a waay which kill promise. Currently there is no way to kill promise!
    cancel() {
        this.tasks.forEach((task) =>
            task.cancel());
    }

    async wait(complete = true) {
        if (complete && this.tasksLeft != 0) {
            await Promise.all(this.tasks);
        }
    }
}